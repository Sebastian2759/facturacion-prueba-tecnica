param(
    [Parameter(Mandatory=$true)][string]$Stage,
    [Parameter(Mandatory=$true)][string]$Version,
    [switch]$FunctionalOnly,
    [switch]$PackageOnly
)
. (Join-Path $PSScriptRoot 'common.ps1')
if ($FunctionalOnly -and $PackageOnly) { throw 'Seleccione una sola fase.' }
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'La verificación integral requiere el job de CI; use -PrepareOnly para preparar el payload localmente.' }
if (-not $FunctionalOnly -and $env:CI_QUALITY_GATE -ne 'passed') { throw 'La entrega final requiere todos los controles previos de CI aprobados.' }
if (-not $PackageOnly -and (-not $env:CI_SQL_PASSWORD -or -not $env:Database__AdminConnectionString)) { throw 'La verificación requiere SQL efímero y las pruebas completas de CI.' }
if (-not (Get-Command gitleaks -ErrorAction SilentlyContinue) -or -not (Get-Command trivy -ErrorAction SilentlyContinue)) { throw 'Faltan los analizadores verificados.' }
$artifactRoot = [IO.Path]::GetFullPath((Join-Path $script:ReleaseRoot 'artifacts')) + [IO.Path]::DirectorySeparatorChar
$stagePath = [IO.Path]::GetFullPath($Stage)
if (-not $stagePath.StartsWith($artifactRoot, [StringComparison]::Ordinal)) { throw 'El paquete debe estar bajo artifacts del repositorio.' }
$manifestPath = Join-Path $stagePath 'release.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$receiptPath = Join-Path $stagePath 'docs/functional-evidence.json'
if ($manifest.version -ne $Version) { throw 'La versión solicitada no corresponde al payload.' }
function Get-PayloadDigest {
    param([string]$PayloadRoot, [string]$ReceiptPath)
    $relativePaths = [string[]]@(Get-ChildItem -LiteralPath $PayloadRoot -Recurse -Force -File |
        Where-Object { -not $_.FullName.Equals($ReceiptPath, [StringComparison]::Ordinal) } |
        ForEach-Object { [IO.Path]::GetRelativePath($PayloadRoot, $_.FullName).Replace('\', '/') })
    [Array]::Sort($relativePaths, [StringComparer]::Ordinal)
    $entries = foreach ($relative in $relativePaths) {
        $hash = (Get-FileHash -LiteralPath (Join-Path $PayloadRoot $relative) -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $relative"
    }
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        $digest = $sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes(($entries -join "`n") + "`n"))
        return [BitConverter]::ToString($digest).Replace('-', '').ToLowerInvariant()
    } finally { $sha256.Dispose() }
}
if (-not $PackageOnly) {
$env:Database__Name = 'FacturacionCI_ReleaseDemo'
$env:Database__RuntimePassword = New-LocalSecret
Write-Host "::add-mask::$($env:Database__RuntimePassword)"
Invoke-Checked 'dotnet' @((Join-Path $stagePath 'database/migrator/DatabaseMigrator.dll'), 'init')
$runtimeChecks = @"
SET NOCOUNT ON;
DECLARE @externalScripts int = (SELECT CONVERT(int, value_in_use) FROM sys.configurations WHERE name=N'external scripts enabled');
SELECT N'external scripts enabled', @externalScripts;
IF @externalScripts IS NULL OR @externalScripts <> 0 THROW 51000, 'La ejecución de scripts externos debe estar deshabilitada.', 1;
USE [FacturacionCI_ReleaseDemo];
EXECUTE AS LOGIN=N'facturacion_runtime';
DECLARE @executeExternal int = HAS_PERMS_BY_NAME(DB_NAME(), N'DATABASE', N'EXECUTE ANY EXTERNAL SCRIPT');
DECLARE @alterLanguage int = HAS_PERMS_BY_NAME(DB_NAME(), N'DATABASE', N'ALTER ANY EXTERNAL LANGUAGE');
DECLARE @alterLibrary int = HAS_PERMS_BY_NAME(DB_NAME(), N'DATABASE', N'ALTER ANY EXTERNAL LIBRARY');
SELECT N'runtime EXECUTE ANY EXTERNAL SCRIPT', @executeExternal;
SELECT N'runtime ALTER ANY EXTERNAL LANGUAGE', @alterLanguage;
SELECT N'runtime ALTER ANY EXTERNAL LIBRARY', @alterLibrary;
REVERT;
IF @executeExternal IS NULL OR @executeExternal <> 0 OR @alterLanguage IS NULL OR @alterLanguage <> 0 OR @alterLibrary IS NULL OR @alterLibrary <> 0
    THROW 51000, 'El runtime no debe administrar ni ejecutar extensiones externas.', 1;
"@
$runtimeEvidence = Invoke-ContainerSql 'facturacion-ci-sql' $runtimeChecks
Write-Utf8 (Join-Path $script:ReleaseRoot 'artifacts/security/sql-runtime-configuration.txt') ($runtimeEvidence -join "`n")
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
GO
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
    $reportDirectory = Join-Path $script:ReleaseRoot 'artifacts/security'
    $cacheEvidencePath = Join-Path $reportDirectory 'api-cache-policy.json'
    & (Join-Path $PSScriptRoot 'smoke-local.ps1') -CacheEvidencePath $cacheEvidencePath
    $apiSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    Invoke-RestMethod 'http://127.0.0.1:5080/api/v1/session' -WebSession $apiSession | Out-Null
    Invoke-WebRequest 'http://127.0.0.1:5080/openapi/v1.json' -WebSession $apiSession -OutFile (Join-Path $script:ReleaseRoot 'artifacts/openapi.json')
    $env:E2E_LIVE = '1'
    $env:E2E_BASE_URL = 'http://127.0.0.1:5080'
    Push-Location (Join-Path $script:ReleaseRoot 'Frontend')
    try {
        Invoke-Checked 'npm' @('run', 'api:check', '--', '../artifacts/openapi.json')
        Invoke-Checked 'npx' @('--no-install', 'playwright', 'test', 'e2e/local-live.spec.ts')
    }
    finally { Pop-Location; Remove-Item Env:E2E_LIVE; Remove-Item Env:E2E_BASE_URL }
    $lock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'toolchain-lock.json') -Raw | ConvertFrom-Json
    Invoke-Checked 'chmod' @('777', $reportDirectory)
    & docker run --rm --network host -v "${reportDirectory}:/zap/wrk:rw" -v "${PSScriptRoot}/zap-local.tsv:/zap/config.tsv:ro" $lock.images.zap zap-baseline.py -t 'http://127.0.0.1:5080' -c '/zap/config.tsv' -r zap.html -J zap.json -m 1
    $zapExitCode = $LASTEXITCODE
    & (Join-Path $PSScriptRoot 'assert-zap-report.ps1') -ReportPath (Join-Path $reportDirectory 'zap.json') -ScannerExitCode $zapExitCode -StaticRoot (Join-Path $stagePath 'app/wwwroot') -CacheEvidencePath $cacheEvidencePath -DecisionPath (Join-Path $reportDirectory 'zap-gate.json')
} finally {
    Invoke-Checked 'docker' @('rm', '-f', 'facturacion-ci-app')
}
$receipt = [ordered]@{
    status = 'functional-verified-not-authorized-for-release'
    runId = $env:GITHUB_RUN_ID; runAttempt = $env:GITHUB_RUN_ATTEMPT
    sourceCommit = $manifest.sourceCommit; version = $manifest.version
    payloadSha256 = Get-PayloadDigest $stagePath $receiptPath
    verifiedUtc = [DateTime]::UtcNow.ToString('o')
}
Write-Utf8 $receiptPath ($receipt | ConvertTo-Json)
Copy-Item -LiteralPath $receiptPath -Destination (Join-Path $script:ReleaseRoot 'artifacts/functional-evidence.json')
}
if ($FunctionalOnly) { Write-Host 'Evidencia funcional generada. Esta fase no autoriza ni produce un ZIP.'; return }
if (-not (Test-Path -LiteralPath $receiptPath)) { throw 'Falta el recibo de verificación funcional.' }
$receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
if ($receipt.status -ne 'functional-verified-not-authorized-for-release' -or
    $receipt.runId -ne $env:GITHUB_RUN_ID -or $receipt.runAttempt -ne $env:GITHUB_RUN_ATTEMPT -or
    $receipt.sourceCommit -ne $manifest.sourceCommit -or $receipt.version -ne $manifest.version -or
    $receipt.payloadSha256 -ne (Get-PayloadDigest $stagePath $receiptPath)) {
    throw 'La evidencia funcional no corresponde al run o payload actual.'
}
Invoke-Checked 'trivy' @('image', '--severity', 'HIGH,CRITICAL', '--exit-code', '1', 'facturacion-local-ci')
Invoke-Checked 'trivy' @('image', '--severity', 'HIGH,CRITICAL', '--exit-code', '1', 'facturacion-migrator-ci')
$sqlDecisionPath = Join-Path $stagePath 'docs/sql-risk-decision.json'
$securityRoot = Join-Path $script:ReleaseRoot 'artifacts/security'
$runtimeSummary = @(Get-Content -LiteralPath (Join-Path $securityRoot 'runtime-images-summary.json') -Raw | ConvertFrom-Json)
$sqlScans = @($runtimeSummary | Where-Object { $_.name -eq 'sqlserver' })
if ($sqlScans.Count -ne 1 -or -not $sqlScans[0].acceptanceValidated) { throw 'Falta la decisión SQL de esta ejecución.' }
& (Join-Path $PSScriptRoot 'assert-sql-risk.ps1') -ReportPath (Join-Path $securityRoot 'trivy-sqlserver.json') -ScannerExitCode $sqlScans[0].exitCode -Image $manifest.images.sqlserver -PolicyPath (Join-Path $stagePath 'docs/sql-risk-acceptance.json') -Profile $manifest.profile -DecisionPath $sqlDecisionPath
$sqlDecision = Get-Content -LiteralPath $sqlDecisionPath -Raw | ConvertFrom-Json
Invoke-Checked 'gitleaks' @('dir', $stagePath, '--redact', '--report-format', 'json', '--report-path', (Join-Path $script:ReleaseRoot 'artifacts/security/package-secrets.json'))
Invoke-Checked 'trivy' @('fs', '--scanners', 'vuln,misconfig,secret', '--severity', 'HIGH,CRITICAL', '--exit-code', '1', $stagePath)
Invoke-Checked 'trivy' @('fs', '--format', 'cyclonedx', '--output', (Join-Path $stagePath 'SBOM.cdx.json'), $stagePath)
$manifest.status = 'release-verified-with-accepted-risk'
$manifest | Add-Member -NotePropertyName sqlRiskAcceptance -NotePropertyValue $sqlDecision
$manifest | Add-Member -NotePropertyName windowsDockerValidation -NotePropertyValue 'pending-manual-verification'
$manifest.backupVerified = $true
Write-Utf8 $manifestPath ($manifest | ConvertTo-Json -Depth 8)
Write-Utf8 (Join-Path $stagePath 'docs/release-evidence.md') "# Evidencia de generación`n`nCommit: $($manifest.sourceCommit). Verificación UTC: $([DateTime]::UtcNow.ToString('o')).`n`nEl job generador completó: backup CHECKSUM y VERIFYONLY, restauración en otra base, tablas/historial EF y ausencia de registros, CHECKDB, factura ficticia creada y consultada vía API y navegador real tras restaurar, repetición idempotente, sesión/CSRF, rechazo de Accounts sin configurar, Gitleaks del paquete, Trivy y baseline ZAP local. Consulte los reportes y resultados de CI del mismo commit; no es una certificación ni un pentest exhaustivo.`n"
Add-Content -LiteralPath (Join-Path $stagePath 'docs/release-evidence.md') -Encoding utf8 -Value "`nSQL tiene riesgo aceptado explícitamente: $($sqlDecision.acceptanceId), $($sqlDecision.highFindings) hallazgos HIGH, sin CRITICAL. No significa corrección ni escaneo limpio. Consulte sql-risk-acceptance.md/json y sql-risk-decision.json. Esta evidencia procede de CI Linux; el arranque completo en Docker Desktop Windows permanece pendiente. La caducidad limita nuevas entregas; no desactiva una instalación existente."
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
