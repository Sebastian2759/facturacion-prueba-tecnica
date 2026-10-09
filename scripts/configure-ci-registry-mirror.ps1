Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_OS -ne 'Linux' -or $env:CI_RUNNER_ENVIRONMENT -ne 'github-hosted' -or -not $IsLinux) {
    throw 'La configuración del daemon sólo está autorizada en el runner Linux efímero de GitHub Actions.'
}
$osRelease = Get-Content -LiteralPath '/etc/os-release' -Raw
if ($osRelease -notmatch '(?m)^ID=ubuntu\r?$' -or $osRelease -notmatch '(?m)^VERSION_ID="24\.04"\r?$') {
    throw 'Se requiere el runner Ubuntu 24.04 declarado por el workflow.'
}
if (-not $env:RUNNER_TEMP -or -not (Test-Path -LiteralPath $env:RUNNER_TEMP -PathType Container)) {
    throw 'Falta el directorio temporal de GitHub Actions.'
}
function Invoke-MirrorCommand {
    param([string]$File, [string[]]$Arguments)
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$File terminó con código $LASTEXITCODE al configurar el espejo de CI." }
}
$daemonPath = '/etc/docker/daemon.json'
$configuration = @{}
if (Test-Path -LiteralPath $daemonPath) {
    $content = Invoke-MirrorCommand 'sudo' @('-n', 'cat', '--', $daemonPath)
    $configuration = ($content -join "`n") | ConvertFrom-Json -AsHashtable
    if ($configuration -isnot [Collections.IDictionary]) { throw 'La configuración Docker existente debe ser un objeto JSON.' }
}
$mirror = 'https://mirror.gcr.io'
$existingMirrors = @()
if ($configuration.Contains('registry-mirrors')) {
    if ($configuration['registry-mirrors'] -isnot [Array]) { throw 'registry-mirrors debe ser un arreglo; no se sustituye una configuración inválida.' }
    $existingMirrors = @($configuration['registry-mirrors'])
    foreach ($entry in $existingMirrors) {
        if ($entry -isnot [string] -or [string]::IsNullOrWhiteSpace($entry)) { throw 'El arreglo de mirrors existente contiene una entrada inválida.' }
    }
}
$configuration['registry-mirrors'] = @($mirror) + @($existingMirrors | Where-Object { $_.TrimEnd('/') -ne $mirror })
$temporaryPath = Join-Path $env:RUNNER_TEMP ('facturacion-docker-daemon-' + [Guid]::NewGuid().ToString('N') + '.json')
try {
    [IO.File]::WriteAllText($temporaryPath, ($configuration | ConvertTo-Json -Depth 50), (New-Object Text.UTF8Encoding $false))
    Invoke-MirrorCommand 'sudo' @('-n', 'dockerd', '--validate', '--config-file', $temporaryPath) | Out-Null
    Invoke-MirrorCommand 'sudo' @('-n', 'install', '--mode=0600', '--owner=root', '--group=root', '--', $temporaryPath, $daemonPath)
    Invoke-MirrorCommand 'sudo' @('-n', 'systemctl', 'restart', 'docker')
    $reportedJson = Invoke-MirrorCommand 'docker' @('info', '--format', '{{json .RegistryConfig.Mirrors}}')
    $reportedMirrors = ($reportedJson -join "`n") | ConvertFrom-Json
    if (-not (@($reportedMirrors) | Where-Object { [string]$_.TrimEnd('/') -eq $mirror })) {
        throw 'Docker no confirmó el espejo público requerido; el paso no puede aprobarse.'
    }
} finally {
    if (Test-Path -LiteralPath $temporaryPath) { Remove-Item -LiteralPath $temporaryPath -Force }
}
Write-Host 'Espejo público mirror.gcr.io confirmado en el daemon del runner. Referencias y digests de imágenes sin cambios.'
