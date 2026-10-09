param([switch]$NoBrowser)
. (Join-Path $PSScriptRoot 'common.ps1')
if ($env:OS -eq 'Windows_NT' -and -not [Environment]::Is64BitOperatingSystem) { throw 'La entrega requiere Windows de 64 bits.' }
if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64' -or $env:PROCESSOR_ARCHITEW6432 -eq 'ARM64') { throw 'Esta entrega está destinada a x64; ARM64 requiere una validación independiente.' }
& (Join-Path $PSScriptRoot 'verify-release.ps1') -Root $script:ReleaseRoot
Assert-Docker
Initialize-LocalSettings
Write-Host 'Descargando los contenedores oficiales y preparando la base local...'
Invoke-Compose @('up', '-d', '--wait', '--wait-timeout', '240', 'database')
Invoke-Compose @('build', 'migrator', 'app')
Invoke-Compose @('run', '--rm', 'migrator', 'init')
Invoke-Compose @('up', '-d', 'app')
$ready = $false
for ($attempt = 0; $attempt -lt 40; $attempt++) {
    try {
        $response = Invoke-WebRequest -Uri 'http://127.0.0.1:5080/health/live' -UseBasicParsing -TimeoutSec 3
        if ($response.StatusCode -eq 200) { $ready = $true; break }
    } catch { Start-Sleep -Seconds 2 }
}
if (-not $ready) { throw 'La aplicación no respondió. Ejecute diagnosticar.cmd; no elimine los volúmenes.' }
Write-Host 'Lista: http://127.0.0.1:5080 . Sus facturas se conservan al detener y volver a iniciar.'
if (-not $NoBrowser) { Start-Process 'http://127.0.0.1:5080' }
