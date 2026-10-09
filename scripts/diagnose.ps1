. (Join-Path $PSScriptRoot 'common.ps1')
Write-Host 'Diagnóstico local. No incluye contraseñas, variables de entorno ni cuerpos de solicitudes.'
Assert-Docker
Invoke-Compose @('ps')
try { & (Join-Path $PSScriptRoot 'verify-release.ps1') -Root $script:ReleaseRoot } catch { Write-Warning $_.Exception.Message }
try {
    $response = Invoke-WebRequest -Uri 'http://127.0.0.1:5080/health/live' -UseBasicParsing -TimeoutSec 5
    Write-Host "HTTP de salud: $($response.StatusCode)"
} catch { Write-Warning 'La aplicación no responde en 127.0.0.1:5080. Confirme puerto, motor Linux y memoria de Docker.' }
