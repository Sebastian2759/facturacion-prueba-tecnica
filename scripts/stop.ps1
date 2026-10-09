. (Join-Path $PSScriptRoot 'common.ps1')
Assert-Docker
Invoke-Compose @('stop')
Write-Host 'Aplicación detenida. La base de datos y la configuración permanecen intactas.'
