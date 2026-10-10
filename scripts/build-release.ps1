param([string]$Version = '1.0.0', [switch]$PrepareOnly)
. (Join-Path $PSScriptRoot 'common.ps1')
if ($Version -notmatch '^\d+\.\d+\.\d+([.-][A-Za-z0-9.-]+)?$') { throw 'Versión no válida.' }
$stage = Join-Path $script:ReleaseRoot "artifacts/package-$Version"
if (Test-Path -LiteralPath $stage) { throw 'Ya existe el directorio de paquete. Use otra versión o retire esa salida revisándola antes.' }
[IO.Directory]::CreateDirectory($stage) | Out-Null
Push-Location $script:ReleaseRoot
try {
    $commit = (& git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'La entrega requiere un commit Git identificable.' }
    if ((& git status --porcelain --untracked-files=normal)) { throw 'El árbol debe estar limpio para que la entrega corresponda al commit indicado.' }
    $npm = if ($env:OS -eq 'Windows_NT') { 'npm.cmd' } else { 'npm' }
    Push-Location Frontend
    try { Invoke-Checked $npm @('ci'); Invoke-Checked $npm @('run', 'build') } finally { Pop-Location }
    Invoke-Checked 'dotnet' @('publish', 'Api/LocalEvaluation/LocalEvaluation.csproj', '-c', 'Release', '-r', 'linux-x64', '--self-contained', 'false', '-p:UseAppHost=false', '-p:DebugType=None', '-p:RestoreLockedMode=true', '-o', (Join-Path $stage 'app'))
    Invoke-Checked 'dotnet' @('publish', 'Tools/DatabaseMigrator/DatabaseMigrator.csproj', '-c', 'Release', '-r', 'linux-x64', '--self-contained', 'false', '-p:UseAppHost=false', '-p:DebugType=None', '-p:RestoreLockedMode=true', '-o', (Join-Path $stage 'database/migrator'))
    $settingsPath = Join-Path $stage 'app/appsettings.json'
    $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    if (-not $settings.PSObject.Properties['Release']) {
        $settings | Add-Member -NotePropertyName Release -NotePropertyValue ([pscustomobject]@{})
    }
    $settings.Release | Add-Member -NotePropertyName Commit -NotePropertyValue $commit -Force
    $settings.Release | Add-Member -NotePropertyName Version -NotePropertyValue $Version -Force
    Write-Utf8 $settingsPath ($settings | ConvertTo-Json -Depth 20)
    $browser = Join-Path $script:ReleaseRoot 'Frontend/dist/facturacion/browser'
    if (-not (Test-Path -LiteralPath (Join-Path $browser 'index.html'))) { throw 'No se encontró el build Angular esperado.' }
    [IO.Directory]::CreateDirectory((Join-Path $stage 'app/wwwroot')) | Out-Null
    Copy-Item -Path (Join-Path $browser '*') -Destination (Join-Path $stage 'app/wwwroot') -Recurse
    foreach ($name in @('scripts', 'docs', 'compose.yaml', 'LEEME.html', 'iniciar.cmd', 'detener.cmd', 'diagnosticar.cmd', 'backup.cmd', 'restaurar.cmd')) {
        Copy-Item -LiteralPath (Join-Path $script:ReleaseRoot $name) -Destination $stage -Recurse
    }
    # Windows PowerShell 5.1 otherwise decodes BOM-less UTF-8 as the local ANSI code page.
    $windowsScriptEncoding = New-Object Text.UTF8Encoding $true
    Get-ChildItem -LiteralPath (Join-Path $stage 'scripts') -Recurse -File -Filter '*.ps1' | ForEach-Object {
        $scriptText = [IO.File]::ReadAllText($_.FullName, [Text.Encoding]::UTF8)
        [IO.File]::WriteAllText($_.FullName, $scriptText, $windowsScriptEncoding)
    }
    Copy-Item -LiteralPath (Join-Path $script:ReleaseRoot 'docs/delivery.md') -Destination (Join-Path $stage 'README.md')
    $deliveryReadme = Join-Path $stage 'README.md'
    Write-Utf8 $deliveryReadme ((Get-Content -LiteralPath $deliveryReadme -Raw).Replace('(sql-risk-acceptance.md)', '(docs/sql-risk-acceptance.md)'))
    [IO.Directory]::CreateDirectory((Join-Path $stage 'source')) | Out-Null
    $tracked = & git ls-files
    foreach ($relative in $tracked) {
        if ($relative -match '(^|/)(bin|obj|node_modules|artifacts|TestResults|backups|\.git|\.tools)(/|$)' -or $relative -match '(\.env$|\.(pfx|key|pem|bak)$)') { continue }
        $destination = Join-Path (Join-Path $stage 'source') $relative
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
        Copy-Item -LiteralPath (Join-Path $script:ReleaseRoot $relative) -Destination $destination
    }
    Write-Utf8 (Join-Path $stage 'database/README.md') "# Base de evaluación`n`ndemo.bak contiene únicamente el esquema migrado, sin facturas ni datos personales. Los ejemplos de la pantalla pública son fixtures de la aplicación. La inicialización normal aplica las migraciones con un usuario administrativo separado; la API usa facturacion_runtime sin DDL. El backup de entrega se incluye únicamente después de restaurarlo y verificarlo en CI.`n"
    $lock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'toolchain-lock.json') -Raw | ConvertFrom-Json
    $manifest = [ordered]@{
        version = $Version; sourceCommit = $commit; profile = 'local-evaluation'; architecture = 'linux-x64-on-windows-docker'
        status = 'prepared-not-verified'; createdUtc = [DateTime]::UtcNow.ToString('o'); sdk = (& dotnet --version).Trim()
        images = $lock.images; database = 'empty-migrated-schema'; backupVerified = $false
    }
    Write-Utf8 (Join-Path $stage 'release.json') ($manifest | ConvertTo-Json -Depth 8)
    Write-Host "Payload preparado en $stage. No es una entrega final hasta verificar SQL, backup, smoke y seguridad."
    if (-not $PrepareOnly) { & (Join-Path $PSScriptRoot 'verify-and-package.ps1') -Stage $stage -Version $Version }
} finally { Pop-Location }
