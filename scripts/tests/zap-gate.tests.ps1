param([string]$RegressionReportPath)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $repo ('artifacts/zap-gate-tests-' + [Guid]::NewGuid().ToString('N'))
$staticRoot = Join-Path $testRoot 'wwwroot'
New-Item -ItemType Directory -Force $staticRoot | Out-Null
[IO.File]::WriteAllText((Join-Path $staticRoot 'main-EXAMPLE.js'), '/* public test fixture */')
[IO.File]::WriteAllText((Join-Path $staticRoot 'index.html'), '<!doctype html><title>Fixture</title>')
$cachePath = Join-Path $testRoot 'synthetic-api-cache-policy.json'
$checks = @(
    @{ method = 'GET'; path = '/api/v1/session'; status = 200; cacheControl = 'no-store' },
    @{ method = 'POST'; path = '/api/v1/invoices'; status = 201; cacheControl = 'no-store' },
    @{ method = 'GET'; path = '/api/v1/invoices/1'; status = 200; cacheControl = 'no-store' },
    @{ method = 'GET'; path = '/api/v1/invoices'; status = 200; cacheControl = 'no-store' },
    @{ method = 'POST'; path = '/api/v1/invoices'; status = 200; cacheControl = 'no-store' },
    @{ method = 'GET'; path = '/api/v1/accounts?mode=simulated'; status = 200; cacheControl = 'no-store' },
    @{ method = 'GET'; path = '/api/v1/accounts?mode=real'; status = 403; cacheControl = 'no-store' }
)
function Save-Cache {
    [IO.File]::WriteAllText($cachePath, (@{ baseUrl = 'http://127.0.0.1:5080'; checks = $checks } | ConvertTo-Json -Depth 6))
}
Save-Cache
function New-Alert {
    param([string]$Rule, [string]$Reference, [int]$Risk, [string]$Path = '/', [string]$Method = 'GET')
    return [pscustomobject]@{
        pluginid = $Rule; alertRef = $Reference; riskcode = [string]$Risk
        instances = @([pscustomobject]@{ uri = 'http://127.0.0.1:5080' + $Path; method = $Method })
    }
}
function Save-Report {
    param([object[]]$Alerts)
    $reportPath = Join-Path $testRoot 'synthetic-zap.json'
    [IO.File]::WriteAllText($reportPath, (@{ site = @(@{ '@name' = 'http://127.0.0.1:5080'; alerts = @($Alerts) }) } | ConvertTo-Json -Depth 10))
    return $reportPath
}
$results = New-Object 'Collections.Generic.List[object]'
function Assert-Gate {
    param([string]$Name, [string]$ReportPath, [bool]$ShouldPass, [int]$ExitCode = 2, [string]$ExpectedError)
    $failure = $null
    try {
        & (Join-Path $repo 'scripts/assert-zap-report.ps1') -ReportPath $ReportPath -ScannerExitCode $ExitCode -StaticRoot $staticRoot -CacheEvidencePath $cachePath 6>$null
    } catch { $failure = $_.Exception.Message }
    if (($null -eq $failure) -ne $ShouldPass) { throw "Gate regresión: $Name esperaba pass=$ShouldPass; error=$failure" }
    if ($ExpectedError -and $failure -notmatch $ExpectedError) { throw "Gate regresión: $Name no falló por la condición esperada: $failure" }
    $results.Add([pscustomobject]@{ name = $Name; result = 'passed'; gateExpected = $(if ($ShouldPass) { 'accept' } else { 'reject' }) })
}
$informative = @(
    (New-Alert '10109' '10109' 0),
    (New-Alert '10049' '10049-1' 0 '/api/v1/invoices'),
    (New-Alert '10049' '10049-3' 0 '/main-EXAMPLE.js')
)
Assert-Gate 'Reviewed informational alerts and exact static asset' (Save-Report $informative) $true
Assert-Gate 'Explicit empty alert array and successful scan' (Save-Report @()) $true 0
Assert-Gate 'Warnings without alert data are incomplete' (Save-Report @()) $false 2 'warnings'
Assert-Gate 'Existing exact loopback exceptions' (Save-Report @((New-Alert '10035' '10035-1' 1), (New-Alert '10011' '10011' 1))) $true
foreach ($path in @('/api/v1/invoices', '/', '/index.html', '/missing.js', '/MAIN-EXAMPLE.js', '/main-EXAMPLE.js?session=x', '/main-EXAMPLE.js#x')) {
    Assert-Gate "Cacheable nonpublic or inexact route $path" (Save-Report @((New-Alert '10049' '10049-3' 0 $path))) $false 2 '10049-3'
}
Assert-Gate 'POST response is never a static cache exception' (Save-Report @((New-Alert '10049' '10049-3' 0 '/main-EXAMPLE.js' 'POST'))) $false 2 '10049-3'
Assert-Gate 'New low risk blocks even when scanner returns success' (Save-Report @((New-Alert '99999' '99999' 1))) $false 0 '99999'
Assert-Gate 'Unknown informative alert requires review' (Save-Report @((New-Alert '99999' '99999' 0))) $false 2 '99999'
Assert-Gate 'Existing exclusion with higher risk blocks' (Save-Report @((New-Alert '10011' '10011' 3))) $false 2 '10011'
Assert-Gate 'Unreviewed HSTS subtype blocks' (Save-Report @((New-Alert '10035' '10035-8' 1))) $false 2 '10035-8'
Assert-Gate 'Informative rule with elevated severity blocks' (Save-Report @((New-Alert '10109' '10109' 1))) $false 2 '10109'
Assert-Gate 'Scanner error cannot be approved by a benign report' (Save-Report $informative) $false 3 'exit 3'
Assert-Gate 'Scanner FAIL cannot be approved by a benign report' (Save-Report $informative) $false 1 'exit 1'
$malformed = Join-Path $testRoot 'malformed.json'
foreach ($raw in @('{', '{}', '{"site":[]}', '{"site":[{"@name":"http://127.0.0.1:5080"}]}', '{"site":[{"@name":"http://127.0.0.1:5080","alerts":{}}]}')) {
    [IO.File]::WriteAllText($malformed, $raw)
    Assert-Gate "Incomplete JSON $raw" $malformed $false 0
}
$unknownRisk = New-Alert '10109' '10109' 0
$unknownRisk.riskcode = 'unknown'
Assert-Gate 'Unknown risk cannot be coerced into informational' (Save-Report @($unknownRisk)) $false 2 'severidad'
$external = New-Alert '10109' '10109' 0
$external.instances[0].uri = 'https://example.invalid/'
Assert-Gate 'Foreign origin is rejected' (Save-Report @($external)) $false 2 'origen'
$checks[0].cacheControl = 'public, max-age=3600'
Save-Cache
Assert-Gate 'API cache regression blocks all informational approval' (Save-Report $informative) $false 2 'no-store'
$checks[0].cacheControl = 'no-store, public'
Save-Cache
Assert-Gate 'Contradictory cache directives block' (Save-Report $informative) $false 2 'no-store'
$checks[0].cacheControl = 'no-store'
$checks[0].path = '/api/v1/other'
Save-Cache
Assert-Gate 'Incomplete endpoint coverage blocks' (Save-Report $informative) $false 2 'todos los tipos'
$checks[0].path = '/api/v1/session'
Save-Cache
if ($RegressionReportPath) {
    Assert-Gate 'Unmodified real run7 CSP and isolation findings block' $RegressionReportPath $false 2 '10055-6.*90004'
}
Write-Host "ZAP gate regression tests passed: $($results.Count). Synthetic fixtures only; no HTTP requests performed."
