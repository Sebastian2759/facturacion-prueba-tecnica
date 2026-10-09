. (Join-Path $PSScriptRoot 'common.ps1')
if ($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_OS -ne 'Linux' -or -not $IsLinux) {
    throw 'El instalador SAST sólo se ejecuta en el runner Linux de GitHub Actions.'
}
if (-not $env:RUNNER_TEMP -or -not (Test-Path -LiteralPath $env:RUNNER_TEMP -PathType Container)) {
    throw 'Falta el directorio temporal aislado del runner.'
}
$lock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'toolchain-lock.json') -Raw | ConvertFrom-Json
$expectedVersion = [string]$lock.tools.semgrep.version
if ($expectedVersion -ne '1.179.0') { throw 'La versión de Semgrep requiere una actualización revisada del script y su lock.' }
$requirements = Join-Path $PSScriptRoot 'security/semgrep-requirements.txt'
if (-not (Test-Path -LiteralPath $requirements -PathType Leaf)) { throw 'Falta el lock completo de Semgrep y sus hashes.' }
$pythonVersion = (Invoke-Checked 'python3.12' @('-I', '-c', 'import sys; assert sys.version_info[:2] == (3, 12); print(sys.version.split()[0])') | Out-String).Trim()
$venv = Join-Path $env:RUNNER_TEMP ('facturacion-semgrep-' + [Guid]::NewGuid().ToString('N'))
Invoke-Checked 'python3.12' @('-I', '-m', 'venv', $venv)
$python = Join-Path $venv 'bin/python'
$semgrep = Join-Path $venv 'bin/semgrep'
Invoke-Checked $python @('-I', '-m', 'pip', '--isolated', '--disable-pip-version-check', 'install', '--require-hashes', '--only-binary=:all:', '--index-url', 'https://pypi.org/simple', '-r', $requirements)
Invoke-Checked $python @('-I', '-m', 'pip', '--isolated', '--disable-pip-version-check', 'check')
$env:SEMGREP_SEND_METRICS = 'off'
$env:SEMGREP_ENABLE_VERSION_CHECK = '0'
$actualVersion = (Invoke-Checked $semgrep @('--version') | Out-String).Trim()
if ($actualVersion -ne $expectedVersion) { throw 'La versión instalada de Semgrep no corresponde al lock.' }
$reportDirectory = Join-Path $script:ReleaseRoot 'artifacts/security'
[IO.Directory]::CreateDirectory($reportDirectory) | Out-Null
$metadata = [ordered]@{
    semgrep = $actualVersion; python = $pythonVersion; index = 'https://pypi.org/simple'
    requirementsSha256 = (Get-FileHash -LiteralPath $requirements -Algorithm SHA256).Hash.ToLowerInvariant()
    hashRequired = $true; wheelsOnly = $true; metrics = 'off'
}
Write-Utf8 (Join-Path $reportDirectory 'typescript-sast-toolchain.json') ($metadata | ConvertTo-Json)
$reportPath = Join-Path $reportDirectory 'typescript-sast.json'
Push-Location $script:ReleaseRoot
try {
    & $semgrep scan --config scripts/security/semgrep.yml --metrics off --disable-version-check --error --json --output $reportPath Frontend/src
    $scanExitCode = $LASTEXITCODE
} finally { Pop-Location }
if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) { throw "Semgrep no produjo el reporte obligatorio (exit $scanExitCode)." }
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if (-not $report.PSObject.Properties['version'] -or $report.version -ne $expectedVersion -or
    -not $report.PSObject.Properties['results'] -or $report.results -isnot [Array] -or
    -not $report.PSObject.Properties['errors'] -or $report.errors -isnot [Array] -or
    -not $report.PSObject.Properties['paths'] -or -not $report.paths.PSObject.Properties['scanned'] -or
    $report.paths.scanned -isnot [Array] -or $report.paths.scanned.Count -eq 0 -or
    $report.paths.scanned -cnotcontains 'Frontend/src/app/core/api-client.ts') {
    throw 'El reporte SAST está incompleto, tiene otra versión o no examinó archivos.'
}
if ($scanExitCode -ne 0 -or $report.results.Count -gt 0 -or $report.errors.Count -gt 0) {
    throw "SAST bloquea la entrega: exit=$scanExitCode, hallazgos=$($report.results.Count), errores=$($report.errors.Count)."
}
Write-Host "Semgrep $actualVersion aprobado: $($report.paths.scanned.Count) archivos, cero hallazgos y cero errores."
