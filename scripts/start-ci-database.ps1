. (Join-Path $PSScriptRoot 'common.ps1')
Assert-Docker
$lock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'toolchain-lock.json') -Raw | ConvertFrom-Json
$password = New-LocalSecret
$env:MSSQL_SA_PASSWORD = $password
Write-Host "::add-mask::$password"
Invoke-Checked 'docker' @('network', 'create', 'facturacion-ci')
Invoke-Checked 'docker' @('run', '--detach', '--name', 'facturacion-ci-sql', '--network', 'facturacion-ci', '--platform', 'linux/amd64', '-e', 'ACCEPT_EULA=Y', '-e', 'MSSQL_PID=Developer', '-e', 'MSSQL_SA_PASSWORD', '-p', '127.0.0.1:1433:1433', $lock.images.sqlserver)
$ready = $false
for ($attempt = 0; $attempt -lt 60; $attempt++) {
    & docker exec facturacion-ci-sql sh -c 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -Q "SELECT 1" -o /dev/null' 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) { $ready = $true; break }
    Start-Sleep -Seconds 2
}
if (-not $ready) { throw 'SQL Server no quedó disponible para las pruebas; no se omiten pruebas de integración.' }
$connection = "Server=127.0.0.1,1433;Database=master;User ID=sa;Password=$password;Encrypt=True;TrustServerCertificate=True"
Write-Host "::add-mask::$connection"
if (-not $env:GITHUB_ENV) { throw 'Este script se ejecuta únicamente en GitHub Actions.' }
Add-Content -LiteralPath $env:GITHUB_ENV -Value "FACTURACION_TEST_SQL_CONNECTION=$connection"
Add-Content -LiteralPath $env:GITHUB_ENV -Value "Database__AdminConnectionString=$connection"
Add-Content -LiteralPath $env:GITHUB_ENV -Value "CI_SQL_PASSWORD=$password"
