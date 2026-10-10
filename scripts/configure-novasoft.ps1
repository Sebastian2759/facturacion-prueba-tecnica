param([switch]$Disable)
. (Join-Path $PSScriptRoot 'common.ps1')
& (Join-Path $PSScriptRoot 'verify-release.ps1') -Root $script:ReleaseRoot
$path = Join-Path $script:ReleaseRoot 'novasoft.local.json'
if (-not (Test-Path -LiteralPath $path)) { Write-Utf8 $path '{}'; Protect-LocalFile $path }
if ($Disable) {
    Write-Utf8 $path '{"Novasoft":{"Enabled":false}}'
} else {
    Write-Host 'Configure solamente las credenciales del servicio de pruebas Novasoft del examen.'
    Write-Host 'Las cuentas creadas en modo real se envían a ese proveedor. Use datos ficticios.'
    $login = Read-Host 'Usuario de integración'
    $connection = Read-Host 'Nombre de conexión'
    $secret = Read-Host 'Contraseña de integración' -AsSecureString
    if ([string]::IsNullOrWhiteSpace($login) -or [string]::IsNullOrWhiteSpace($connection) -or $secret.Length -eq 0) { throw 'Debe completar los tres campos.' }
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
    try {
        $settings = @{ Novasoft = @{ Enabled = $true; UserLogin = $login; ConnectionName = $connection; Password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) } }
        Write-Utf8 $path ($settings | ConvertTo-Json -Depth 4)
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
        $settings = $null
        $secret.Dispose()
    }
}
Protect-LocalFile $path
Write-Host 'Configuración privada guardada. Ejecute detener.cmd y después iniciar.cmd; recargue el navegador.'
Write-Host 'No comparta novasoft.local.json ni lo añada al ZIP. Para deshabilitar: configurar-novasoft.cmd -Disable.'
