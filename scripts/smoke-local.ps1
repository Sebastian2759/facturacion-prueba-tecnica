param([string]$BaseUrl = 'http://127.0.0.1:5080')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($BaseUrl -ne 'http://127.0.0.1:5080') { throw 'El smoke automático sólo está autorizado contra la aplicación local de CI.' }
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$state = Invoke-RestMethod "$BaseUrl/api/v1/session" -WebSession $session
if ($state.data.mode -ne 'local' -or -not $state.data.authenticated -or $state.data.canUseRealAccounts) { throw 'Perfil o permisos locales incorrectos.' }
$headers = @{ 'X-CSRF-TOKEN' = $state.data.csrfToken; Origin = $BaseUrl; 'Idempotency-Key' = [Guid]::NewGuid().ToString() }
$body = @{
    document = '900000001'; firstName = 'Evaluador'; lastName = 'Ficticio'; address = 'Calle ficticia 1'; phone = '3000000000'
    details = @(@{ code = 101; quantity = 2; unitPrice = 250000 })
} | ConvertTo-Json -Depth 5
$created = Invoke-RestMethod "$BaseUrl/api/v1/invoices" -Method Post -ContentType 'application/json' -Body $body -Headers $headers -WebSession $session
if (-not $created.isSuccess -or $created.data.invoice.total -ne 565250) { throw 'El guardado o los totales de la factura de prueba no son correctos.' }
$number = $created.data.invoice.number
$found = Invoke-RestMethod "$BaseUrl/api/v1/invoices/$number" -WebSession $session
if ($found.data.invoice.number -ne $number) { throw 'No se recuperó la factura guardada.' }
$replayed = Invoke-RestMethod "$BaseUrl/api/v1/invoices" -Method Post -ContentType 'application/json' -Body $body -Headers $headers -WebSession $session
if ($replayed.data.invoice.number -ne $number) { throw 'La repetición idempotente generó otro número.' }
$accounts = Invoke-RestMethod "$BaseUrl/api/v1/accounts?mode=simulated" -WebSession $session
if ($accounts.data.mode -ne 'simulated' -or $accounts.data.accounts.Count -lt 1) { throw 'La simulación de Accounts no está disponible.' }
try {
    Invoke-RestMethod "$BaseUrl/api/v1/accounts?mode=real" -WebSession $session | Out-Null
    throw 'La integración real quedó habilitada en el perfil local.'
} catch {
    if ($_.Exception.Response.StatusCode.value__ -ne 403) { throw }
}
Write-Host 'Smoke local correcto: sesión/antiforgery, guardado y consulta SQL, cálculo, idempotencia, Accounts simuladas y bloqueo real.'
