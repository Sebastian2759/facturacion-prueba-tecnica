. (Join-Path $PSScriptRoot 'common.ps1')
if ($env:OS -eq 'Windows_NT') { throw 'Este instalador de herramientas está destinado al runner Linux x64 de CI.' }
$lock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'toolchain-lock.json') -Raw | ConvertFrom-Json
$directory = Join-Path $script:ReleaseRoot '.tools'
[IO.Directory]::CreateDirectory($directory) | Out-Null
foreach ($name in @('gitleaks', 'trivy')) {
    $tool = $lock.tools.$name
    $archive = Join-Path $directory "$name.tar.gz"
    Invoke-WebRequest -Uri $tool.url -OutFile $archive
    if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $tool.sha256) { throw "Integridad de $name inválida." }
    Invoke-Checked 'tar' @('-xzf', $archive, '-C', $directory, $name)
    Invoke-Checked (Join-Path $directory $name) @('--version')
}
if ($env:GITHUB_PATH) { Add-Content -LiteralPath $env:GITHUB_PATH -Value $directory }
