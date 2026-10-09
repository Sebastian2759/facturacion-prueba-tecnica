param([Parameter(Mandatory=$true)][string]$Stage, [Parameter(Mandatory=$true)][string]$Version)
. (Join-Path $PSScriptRoot 'common.ps1')
if ($env:GITHUB_ACTIONS -ne 'true' -or $env:CI_QUALITY_GATE -ne 'passed') { throw 'La entrega final requiere el job de CI completo; use -PrepareOnly para preparar el payload localmente.' }
if (-not $env:CI_SQL_PASSWORD -or -not $env:Database__AdminConnectionString) { throw 'La entrega final requiere el SQL efímero y las pruebas completas de CI.' }
if (-not (Get-Command gitleaks -ErrorAction SilentlyContinue) -or -not (Get-Command trivy -ErrorAction SilentlyContinue)) { throw 'Faltan los analizadores verificados.' }
$artifactRoot = [IO.Path]::GetFullPath((Join-Path $script:ReleaseRoot 'artifacts')) + [IO.Path]::DirectorySeparatorChar
$stagePath = [IO.Path]::GetFullPath($Stage)
if (-not $stagePath.StartsWith($artifactRoot, [StringComparison]::Ordinal)) { throw 'El paquete debe estar bajo artifacts del repositorio.' }
$manifestPath = Join-Path $stagePath 'release.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$env:Database__Name = 'FacturacionCI_ReleaseDemo'
$env:Database__RuntimePassword = New-LocalSecret
Write-Host "::add-mask::$($env:Database__RuntimePassword)"
Invoke-Checked 'dotnet' @((Join-Path $stagePath 'database/migrator/DatabaseMigrator.dll'), 'init')
Invoke-Checked 'docker' @('exec', 'facturacion-ci-sql', 'mkdir', '-p', '/var/opt/mssql/backup')
$sql = @"
USE [FacturacionCI_ReleaseDemo];
IF EXISTS(SELECT 1 FROM dbo.Facturas) OR EXISTS(SELECT 1 FROM dbo.DetalleFactura) THROW 51000, 'El backup de entrega debe estar vacío.', 1;
BACKUP DATABASE [FacturacionCI_ReleaseDemo] TO DISK=N'/var/opt/mssql/backup/demo.bak' WITH CHECKSUM;
RESTORE VERIFYONLY FROM DISK=N'/var/opt/mssql/backup/demo.bak' WITH CHECKSUM;
USE master;
IF DB_ID(N'FacturacionCI_RestoreCheck') IS NOT NULL THROW 51000, 'La base de restauración debe ser nueva.', 1;
RESTORE DATABASE [FacturacionCI_RestoreCheck] FROM DISK=N'/var/opt/mssql/backup/demo.bak' WITH CHECKSUM,
 MOVE N'FacturacionCI_ReleaseDemo' TO N'/var/opt/mssql/data/FacturacionCI_RestoreCheck.mdf',
 MOVE N'FacturacionCI_ReleaseDemo_log' TO N'/var/opt/mssql/data/FacturacionCI_RestoreCheck_log.ldf';
USE [FacturacionCI_RestoreCheck];
IF OBJECT_ID(N'dbo.Facturas') IS NULL OR OBJECT_ID(N'dbo.DetalleFactura') IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory) THROW 51000, 'Esquema restaurado inválido.', 1;
IF EXISTS(SELECT 1 FROM dbo.Facturas) OR EXISTS(SELECT 1 FROM dbo.DetalleFactura) THROW 51000, 'La restauración no debe contener datos.', 1;
DBCC CHECKDB WITH NO_INFOMSGS;
"@
Invoke-ContainerSql 'facturacion-ci-sql' $sql | Out-Null
Invoke-Checked 'docker' @('cp', 'facturacion-ci-sql:/var/opt/mssql/backup/demo.bak', (Join-Path $stagePath 'database/demo.bak'))
$env:Database__Name = 'FacturacionCI_RestoreCheck'
Invoke-Checked 'dotnet' @((Join-Path $stagePath 'database/migrator/DatabaseMigrator.dll'), 'init')
$env:ConnectionStrings__AppDbContext = "Server=facturacion-ci-sql;Database=FacturacionCI_RestoreCheck;User ID=facturacion_runtime;Password=$($env:Database__RuntimePassword);Encrypt=True;TrustServerCertificate=True"
$env:Local__InstallationId = [Guid]::NewGuid().ToString()
$env:ASPNETCORE_ENVIRONMENT = 'LocalEvaluation'
$env:OpenApi__Enabled = 'true'
Invoke-Checked 'docker' @('build', '-t', 'facturacion-local-ci', '-f', (Join-Path $stagePath 'scripts/docker/Dockerfile.local'), $stagePath)
Invoke-Checked 'docker' @('build', '-t', 'facturacion-migrator-ci', '-f', (Join-Path $stagePath 'scripts/docker/Dockerfile.migrator'), $stagePath)
Invoke-Checked 'trivy' @('image', '--severity', 'HIGH,CRITICAL', '--exit-code', '1', 'facturacion-local-ci')
Invoke-Checked 'trivy' @('image', '--severity', 'HIGH,CRITICAL', '--exit-code', '1', 'facturacion-migrator-ci')
$env:Database__AdminConnectionString = "Server=facturacion-ci-sql;Database=master;User ID=sa;Password=$($env:CI_SQL_PASSWORD);Encrypt=True;TrustServerCertificate=True"
Invoke-Checked 'docker' @('run', '--rm', '--network', 'facturacion-ci', '-e', 'Database__AdminConnectionString', '-e', 'Database__Name', '-e', 'Database__RuntimePassword', 'facturacion-migrator-ci', 'init')
Invoke-Checked 'docker' @('run', '--detach', '--name', 'facturacion-ci-app', '--network', 'facturacion-ci', '--read-only', '--tmpfs', '/tmp:rw,noexec,nosuid,size=64m', '--cap-drop', 'ALL', '--security-opt', 'no-new-privileges:true', '-p', '127.0.0.1:5080:8080', '-e', 'ConnectionStrings__AppDbContext', '-e', 'Local__InstallationId', '-e', 'ASPNETCORE_ENVIRONMENT', '-e', 'OpenApi__Enabled', 'facturacion-local-ci')
try {
    $ready = $false
    for ($attempt=0; $attempt -lt 40; $attempt++) {
        try { Invoke-WebRequest 'http://127.0.0.1:5080/health/live' -TimeoutSec 2 | Out-Null; $ready=$true; break } catch { Start-Sleep -Seconds 1 }
    }
    if (-not $ready) { throw 'El host local no inició.' }
    $reportedVersion = Invoke-RestMethod 'http://127.0.0.1:5080/api/v1/version'
    if ($reportedVersion.profile -ne 'local' -or $reportedVersion.commit -ne $manifest.sourceCommit -or $reportedVersion.version -ne $manifest.version) {
        throw 'La versión o el commit de la aplicación no coincide con el manifiesto del paquete.'
    }
    & (Join-Path $PSScriptRoot 'smoke-local.ps1')
    $apiSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    Invoke-RestMethod 'http://127.0.0.1:5080/api/v1/session' -WebSession $apiSession | Out-Null
    Invoke-WebRequest 'http://127.0.0.1:5080/openapi/v1.json' -WebSession $apiSession -OutFile (Join-Path $script:ReleaseRoot 'artifacts/openapi.json')
    $env:E2E_LIVE = '1'
    $env:E2E_BASE_URL = 'http://127.0.0.1:5080'
    Push-Location (Join-Path $script:ReleaseRoot 'Frontend')
    try { Invoke-Checked 'npx' @('--no-install', 'playwright', 'test', 'e2e/local-live.spec.ts') }
    finally { Pop-Location; Remove-Item Env:E2E_LIVE; Remove-Item Env:E2E_BASE_URL }
    $lock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'toolchain-lock.json') -Raw | ConvertFrom-Json
    $reportDirectory = Join-Path $script:ReleaseRoot 'artifacts/security'
    Invoke-Checked 'chmod' @('777', $reportDirectory)
    Invoke-Checked 'docker' @('run', '--rm', '--network', 'host', '-v', "${reportDirectory}:/zap/wrk:rw", '-v', "${PSScriptRoot}/zap-local.tsv:/zap/config.tsv:ro", $lock.images.zap, 'zap-baseline.py', '-t', 'http://127.0.0.1:5080', '-c', '/zap/config.tsv', '-r', 'zap.html', '-J', 'zap.json', '-m', '1')
} finally {
    Invoke-Checked 'docker' @('rm', '-f', 'facturacion-ci-app')
}
Invoke-Checked 'gitleaks' @('dir', $stagePath, '--redact', '--report-format', 'json', '--report-path', (Join-Path $script:ReleaseRoot 'artifacts/security/package-secrets.json'))
Invoke-Checked 'trivy' @('fs', '--scanners', 'vuln,misconfig,secret', '--severity', 'HIGH,CRITICAL', '--exit-code', '1', $stagePath)
Invoke-Checked 'trivy' @('fs', '--format', 'cyclonedx', '--output', (Join-Path $stagePath 'SBOM.cdx.json'), $stagePath)
$manifest.status = 'release-verified'
$manifest.backupVerified = $true
Write-Utf8 $manifestPath ($manifest | ConvertTo-Json -Depth 8)
Write-Utf8 (Join-Path $stagePath 'docs/release-evidence.md') "# Evidencia de generación`n`nCommit: $($manifest.sourceCommit). Verificación UTC: $([DateTime]::UtcNow.ToString('o')).`n`nEl job generador completó: backup CHECKSUM y VERIFYONLY, restauración en otra base, tablas/historial EF y ausencia de registros, CHECKDB, factura ficticia creada y consultada vía API y navegador real tras restaurar, repetición idempotente, sesión/CSRF, Accounts simuladas y rechazo real, Gitleaks del paquete, Trivy y baseline ZAP local. Consulte los reportes y resultados de CI del mismo commit; no es una certificación ni un pentest exhaustivo.`n"
$checksums = Get-ChildItem -LiteralPath $stagePath -Recurse -Force -File | Sort-Object FullName | ForEach-Object {
    $relative = [IO.Path]::GetRelativePath($stagePath, $_.FullName).Replace('\','/')
    "$(($_ | Get-FileHash -Algorithm SHA256).Hash.ToLowerInvariant())  $relative"
}
Write-Utf8 (Join-Path $stagePath 'SHA256SUMS.txt') (($checksums -join "`n") + "`n")
& (Join-Path $PSScriptRoot 'verify-release.ps1') -Root $stagePath
$zip = Join-Path $script:ReleaseRoot "artifacts/Aplicacion-$Version.zip"
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($stagePath, $zip)
$extracted = Join-Path $artifactRoot "extracted-$Version"
[IO.Compression.ZipFile]::ExtractToDirectory($zip, $extracted)
& (Join-Path $PSScriptRoot 'verify-release.ps1') -Root $extracted
Invoke-Checked 'gitleaks' @('dir', $extracted, '--redact')
Write-Utf8 (Join-Path $script:ReleaseRoot 'artifacts/SHA256SUMS.txt') "$((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant())  Aplicacion-$Version.zip`n"
Write-Host "Entrega verificada: $zip"
