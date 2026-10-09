param(
    [Parameter(Mandatory=$true)][string]$ReportPath,
    [Parameter(Mandatory=$true)][int]$ScannerExitCode,
    [Parameter(Mandatory=$true)][string]$StaticRoot,
    [Parameter(Mandatory=$true)][string]$CacheEvidencePath,
    [string]$DecisionPath
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($ScannerExitCode -notin @(0, 2)) { throw "ZAP no completó un baseline evaluable (exit $ScannerExitCode)." }
$report = Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json
if (-not $report.PSObject.Properties['site'] -or $report.site -isnot [Array] -or $report.site.Count -ne 1) { throw 'ZAP debe informar exactamente el sitio local en un arreglo válido.' }
$site = $report.site[0]
if (-not $site.PSObject.Properties['alerts'] -or $site.alerts -isnot [Array]) { throw 'Falta el arreglo alerts; un reporte incompleto no equivale a cero hallazgos.' }
if ($ScannerExitCode -eq 2 -and $site.alerts.Count -eq 0) { throw 'ZAP declaró warnings, pero el reporte no contiene alertas evaluables.' }
if ([string]$site.'@name' -ne 'http://127.0.0.1:5080') { throw 'El reporte no corresponde al sitio local autorizado.' }
$cache = Get-Content -LiteralPath $CacheEvidencePath -Raw | ConvertFrom-Json
if ($cache.baseUrl -ne 'http://127.0.0.1:5080' -or $cache.checks -isnot [Array] -or $cache.checks.Count -lt 7) { throw 'Falta evidencia suficiente de no-store para la API.' }
$required = @{ session = $false; write = $false; detail = $false; list = $false; simulated = $false; forbidden = $false }
foreach ($check in $cache.checks) {
    if ([string]$check.cacheControl -notmatch '(?i)(^|,)\s*no-store\s*(,|$)' -or
        [string]$check.cacheControl -match '(?i)(^|,)\s*(public|s-maxage\s*=|max-age\s*=)') { throw 'Una respuesta API carece de no-store o permite caché compartida.' }
    $key = "$($check.method) $($check.path) $($check.status)"
    if ($key -eq 'GET /api/v1/session 200') { $required.session = $true }
    if ($key -match '^POST /api/v1/invoices (200|201)$') { $required.write = $true }
    if ($key -match '^GET /api/v1/invoices/[1-9][0-9]* 200$') { $required.detail = $true }
    if ($key -eq 'GET /api/v1/invoices 200') { $required.list = $true }
    if ($key -eq 'GET /api/v1/accounts?mode=simulated 200') { $required.simulated = $true }
    if ($key -eq 'GET /api/v1/accounts?mode=real 403') { $required.forbidden = $true }
}
if ($required.Values -contains $false) { throw 'No se comprobaron todos los tipos de respuesta API requeridos.' }
$staticPaths = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
$staticRootPath = [IO.Path]::GetFullPath($StaticRoot)
Get-ChildItem -LiteralPath $staticRootPath -Recurse -File | Where-Object {
    $_.Extension -in @('.js', '.css', '.svg', '.png', '.jpg', '.jpeg', '.webp', '.gif', '.ico', '.woff', '.woff2', '.ttf', '.otf')
} | ForEach-Object {
    $relative = [IO.Path]::GetRelativePath($staticRootPath, $_.FullName).Replace('\', '/')
    if ($relative -notmatch '^(api|openapi|health)(/|$)') { $staticPaths.Add('/' + $relative) | Out-Null }
}
$accepted = New-Object 'Collections.Generic.List[object]'
$rejected = New-Object 'Collections.Generic.List[string]'
foreach ($alert in $site.alerts) {
    if (-not $alert.PSObject.Properties['riskcode'] -or [string]$alert.riskcode -notmatch '^[0-3]$' -or
        -not $alert.PSObject.Properties['pluginid'] -or -not $alert.PSObject.Properties['alertRef'] -or
        -not $alert.PSObject.Properties['instances'] -or $alert.instances -isnot [Array] -or $alert.instances.Count -eq 0) { throw 'Alerta ZAP incompleta o con severidad desconocida.' }
    $risk = [int]$alert.riskcode
    $rule = [string]$alert.pluginid
    $reference = [string]$alert.alertRef
    foreach ($instance in $alert.instances) {
        $uri = $null
        if (-not [Uri]::TryCreate([string]$instance.uri, [UriKind]::Absolute, [ref]$uri) -or
            $uri.Scheme -ne 'http' -or $uri.Host -ne '127.0.0.1' -or $uri.Port -ne 5080 -or $uri.UserInfo) { throw 'ZAP informó una URL fuera del origen local autorizado.' }
        $reason = $null
        if ($risk -eq 1 -and (($rule -eq '10035' -and $reference -eq '10035-1') -or ($rule -eq '10011' -and $reference -eq '10011'))) {
            $reason = 'existing-loopback-http-exclusion'
        } elseif ($risk -eq 0 -and $rule -eq '10109' -and $reference -eq '10109') {
            $reason = 'modern-application-informational'
        } elseif ($risk -eq 0 -and $rule -eq '10049' -and $reference -eq '10049-1') {
            $reason = 'non-storable-response-with-api-no-store-assertions'
        } elseif ($risk -eq 0 -and $rule -eq '10049' -and $reference -eq '10049-3' -and
            [string]$instance.method -in @('GET', 'HEAD') -and -not $uri.Query -and -not $uri.Fragment -and $staticPaths.Contains($uri.AbsolutePath)) {
            $reason = 'exact-published-public-static-asset'
        }
        if ($null -eq $reason) { $rejected.Add("$reference risk=$risk $($uri.AbsolutePath)") }
        else { $accepted.Add([pscustomobject]@{ alertRef = $reference; risk = $risk; path = $uri.AbsolutePath; reason = $reason }) }
    }
}
$decision = [ordered]@{ status = $(if ($rejected.Count -eq 0) { 'passed' } else { 'failed' }); scannerExitCode = $ScannerExitCode; accepted = @($accepted.ToArray()); rejected = @($rejected.ToArray()); apiNoStoreChecked = $true }
if ($DecisionPath) { [IO.File]::WriteAllText($DecisionPath, ($decision | ConvertTo-Json -Depth 6), (New-Object Text.UTF8Encoding $false)) }
if ($rejected.Count -gt 0) { throw ('ZAP bloquea la entrega: ' + ($rejected -join '; ')) }
Write-Host 'ZAP JSON aprobado: sin riesgos nuevos aceptados; informativos revisados por subtipo/ruta y API no-store comprobada.'
