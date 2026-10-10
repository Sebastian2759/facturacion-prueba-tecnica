Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $repo ('artifacts/smoke-cache-tests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $testRoot | Out-Null
$fixtureCacheState = @{ badPath = $null; allowReal = $false }
function Invoke-WebRequest {
    param([Uri]$Uri, [string]$Method, $WebSession, $Headers, [int]$TimeoutSec, [int]$MaximumRedirection, [switch]$SkipHttpErrorCheck, [string]$Body, [string]$ContentType)
    if ($Uri.GetLeftPart([UriPartial]::Authority) -ne 'http://127.0.0.1:5080' -or $MaximumRedirection -ne 0 -or -not $SkipHttpErrorCheck) { throw 'Unexpected HTTP request configuration.' }
    $path = $Uri.PathAndQuery
    $status = 200
    $result = @{ isSuccess = $true; data = @{} }
    switch ($path) {
        '/api/v1/session' { $result.data = @{ mode = 'local'; authenticated = $true; canUseRealAccounts = $false; csrfToken = 'synthetic-test-value' } }
        '/api/v1/invoices' {
            if ($Method -eq 'POST') {
                if ($Headers.Origin -ne 'http://127.0.0.1:5080' -or $Headers['X-CSRF-TOKEN'] -ne 'synthetic-test-value' -or -not $Body) { throw 'Missing write protections.' }
                $status = 201
                $result.data = @{ invoice = @{ number = 1; total = 565250 } }
            }
        }
        '/api/v1/invoices/1' { $result.data = @{ invoice = @{ number = 1 } } }
        '/api/v1/accounts' { if (-not $fixtureCacheState.allowReal) { $status = 403 }; $result = @{ status = $status } }
        default { throw "Unexpected fixture route: $path" }
    }
    $cacheControl = $(if ($path -eq $fixtureCacheState.badPath) { 'public, max-age=3600' } else { 'no-store' })
    return [pscustomobject]@{ StatusCode = $status; Headers = @{ 'Cache-Control' = [string[]]@($cacheControl) }; Content = $result | ConvertTo-Json -Depth 6 }
}
$cachePath = Join-Path $testRoot 'synthetic-api-cache-policy.json'
& (Join-Path $repo 'scripts/smoke-local.ps1') -CacheEvidencePath $cachePath 6>$null
$evidence = Get-Content -LiteralPath $cachePath -Raw | ConvertFrom-Json
if ($evidence.checks.Count -ne 6 -or ($evidence.checks | Where-Object { $_.cacheControl -ne 'no-store' })) { throw 'The smoke did not record all six API cache checks.' }
if ((Get-Content -LiteralPath $cachePath -Raw) -match 'synthetic-test-value|csrf|cookie|Evaluador|565250') { throw 'The cache evidence contains response body or authentication data.' }
$count = 1
foreach ($path in @('/api/v1/session', '/api/v1/invoices', '/api/v1/invoices/1', '/api/v1/accounts')) {
    $fixtureCacheState.badPath = $path
    $failure = $null
    try { & (Join-Path $repo 'scripts/smoke-local.ps1') 6>$null } catch { $failure = $_.Exception.Message }
    if (-not $failure -or $failure -notmatch 'no-store') { throw "Cache regression was not rejected for $path." }
    $count++
}
$fixtureCacheState.badPath = $null
$fixtureCacheState.allowReal = $true
$failure = $null
try { & (Join-Path $repo 'scripts/smoke-local.ps1') 6>$null } catch { $failure = $_.Exception.Message }
if (-not $failure -or $failure -notmatch 'Respuesta inesperada') { throw 'Real Accounts unexpectedly allowed was not rejected.' }
$count++
Write-Host "Smoke cache regression tests passed: $count. HTTP responses are synthetic; no network requests performed."
