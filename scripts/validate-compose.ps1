. (Join-Path $PSScriptRoot 'common.ps1')
Assert-Docker
$directory = Join-Path $script:ReleaseRoot '.runtime'
[IO.Directory]::CreateDirectory($directory) | Out-Null
$emptyEnvironment = Join-Path $directory 'compose-validation.env'
Write-Utf8 $emptyEnvironment ''
$original = @{}
foreach ($key in @('SQL_ADMIN_PASSWORD', 'SQL_RUNTIME_PASSWORD', 'INSTALLATION_ID', 'COMPOSE_PROJECT_NAME')) {
    $original[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
}
try {
    $env:SQL_ADMIN_PASSWORD = New-LocalSecret
    $env:SQL_RUNTIME_PASSWORD = New-LocalSecret
    $env:INSTALLATION_ID = [Guid]::NewGuid().ToString()
    $env:COMPOSE_PROJECT_NAME = 'facturacion_compose_check'
    $serialized = Invoke-Checked 'docker' @('compose', '--env-file', $emptyEnvironment, '-f', (Join-Path $script:ReleaseRoot 'compose.yaml'), '--profile', 'tools', 'config', '--format', 'json')
    $configuration = ($serialized -join "`n") | ConvertFrom-Json
    $lock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'toolchain-lock.json') -Raw | ConvertFrom-Json
    if ($configuration.services.database.image -cne $lock.images.sqlserver) { throw 'La imagen SQL de Compose debe coincidir con el digest analizado y aceptado.' }
    $port = @($configuration.services.app.ports)
    if ($port.Count -ne 1 -or $port[0].host_ip -ne '127.0.0.1' -or $port[0].published -ne '5080') { throw 'La aplicación debe publicar un único puerto 5080 en loopback.' }
    if ($configuration.services.database.PSObject.Properties['ports']) { throw 'SQL Server no debe publicar puertos en la entrega.' }
    foreach ($service in @('app', 'migrator')) {
        if (@($configuration.services.$service.tmpfs).Count -ne 1 -or $configuration.services.$service.tmpfs[0] -ne '/tmp:rw,noexec,nosuid,size=64m') { throw "El montaje temporal de $service no es válido." }
        if (-not $configuration.services.$service.read_only) { throw "El sistema de archivos de $service debe ser de sólo lectura." }
    }
    if (-not $configuration.networks.private.internal) { throw 'La red de los contenedores de evaluación debe ser interna.' }
    Write-Host 'Compose válido: loopback, SQL sin puerto, red interna, lectura sola y tmpfs configurados.'
} finally {
    foreach ($key in $original.Keys) { [Environment]::SetEnvironmentVariable($key, $original[$key], 'Process') }
}
