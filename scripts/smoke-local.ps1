param(
    [string]$BaseUrl = 'http://127.0.0.1:5080',
    [string]$CacheEvidencePath
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($BaseUrl -ne 'http://127.0.0.1:5080') { throw 'El smoke automático sólo está autorizado contra la aplicación local de CI.' }
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$cacheChecks = New-Object 'Collections.Generic.List[object]'
function Invoke-SmokeRequest {
    param([string]$Path, [string]$Method = 'GET', [int[]]$ExpectedStatus = @(200), [hashtable]$Headers = @{}, [string]$Body)
    $arguments = @{
        Uri = "$BaseUrl$Path"; Method = $Method; WebSession = $session; Headers = $Headers
        TimeoutSec = 20; MaximumRedirection = 0; SkipHttpErrorCheck = $true
    }
    if ($Body) { $arguments.Body = $Body; $arguments.ContentType = 'application/json' }
    $response = Invoke-WebRequest @arguments
    $status = [int]$response.StatusCode
    if ($status -notin $ExpectedStatus) { throw "Respuesta inesperada del smoke: $Method $Path ($status)." }
    $cacheControl = [string]::Join(', ', @($response.Headers['Cache-Control']))
    if ($cacheControl -notmatch '(?i)(^|,)\s*no-store\s*(,|$)' -or
        $cacheControl -match '(?i)(^|,)\s*(public|s-maxage\s*=|max-age\s*=)') { throw "Falta no-store o existe una directiva de caché incompatible en $Method $Path." }
    $cacheChecks.Add([pscustomobject]@{ method = $Method; path = $Path; status = $status; cacheControl = $cacheControl })
    return ($response.Content | ConvertFrom-Json)
}
$state = Invoke-SmokeRequest '/api/v1/session'
if ($state.data.mode -ne 'local' -or -not $state.data.authenticated -or $state.data.canUseRealAccounts) { throw 'Perfil o permisos locales incorrectos.' }
$headers = @{ 'X-CSRF-TOKEN' = $state.data.csrfToken; Origin = $BaseUrl; 'Idempotency-Key' = [Guid]::NewGuid().ToString() }
$body = @{
    document = '900000001'; firstName = 'Evaluador'; lastName = 'Ficticio'; address = 'Calle ficticia 1'; phone = '3000000000'
    details = @(@{ code = 101; quantity = 2; unitPrice = 250000 })
} | ConvertTo-Json -Depth 5
$created = Invoke-SmokeRequest '/api/v1/invoices' -Method POST -ExpectedStatus @(200, 201) -Body $body -Headers $headers
if (-not $created.isSuccess -or $created.data.invoice.total -ne 565250) { throw 'El guardado o los totales de la factura de prueba no son correctos.' }
$number = $created.data.invoice.number
$found = Invoke-SmokeRequest "/api/v1/invoices/$number"
if ($found.data.invoice.number -ne $number) { throw 'No se recuperó la factura guardada.' }
$listed = Invoke-SmokeRequest '/api/v1/invoices'
if (-not $listed.isSuccess) { throw 'No se pudo consultar la lista de facturas.' }
$replayed = Invoke-SmokeRequest '/api/v1/invoices' -Method POST -ExpectedStatus @(200, 201) -Body $body -Headers $headers
if ($replayed.data.invoice.number -ne $number) { throw 'La repetición idempotente generó otro número.' }
Invoke-SmokeRequest '/api/v1/accounts' -ExpectedStatus @(403) | Out-Null
if ($CacheEvidencePath) {
    $evidence = [ordered]@{ baseUrl = $BaseUrl; checks = @($cacheChecks.ToArray()) }
    [IO.File]::WriteAllText($CacheEvidencePath, ($evidence | ConvertTo-Json -Depth 5), (New-Object Text.UTF8Encoding $false))
}
Write-Host 'Smoke local correcto: sesión/antiforgery, guardado/lista/detalle SQL, cálculo, idempotencia, bloqueo de Accounts sin configurar y no-store en las seis respuestas.'
