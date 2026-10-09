. (Join-Path $PSScriptRoot 'common.ps1')
$reports = Join-Path $script:ReleaseRoot 'artifacts/security'
[IO.Directory]::CreateDirectory($reports) | Out-Null
Push-Location $script:ReleaseRoot
try {
    $output = & dotnet list Example.sln package --vulnerable --include-transitive --format json 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo completar la auditoría NuGet; una consulta fallida no equivale a cero hallazgos.' }
    $text = $output -join "`n"
    Write-Utf8 (Join-Path $reports 'nuget-vulnerabilities.json') $text
    $report = $text | ConvertFrom-Json
    foreach ($project in $report.projects) {
        foreach ($framework in $project.frameworks) {
            foreach ($kind in @('topLevelPackages', 'transitivePackages')) {
                $property = $framework.PSObject.Properties[$kind]
                if ($null -ne $property -and @($property.Value).Count -gt 0) { throw 'NuGet detectó vulnerabilidades. Todas las severidades bloquean la entrega.' }
            }
        }
    }
    Invoke-Checked 'dotnet' @('list', 'Example.sln', 'package', '--deprecated')
    Invoke-Checked 'dotnet' @('list', 'Example.sln', 'package', '--outdated')
    Push-Location (Join-Path $script:ReleaseRoot 'Frontend')
    try {
        $npm = if ($env:OS -eq 'Windows_NT') { 'npm.cmd' } else { 'npm' }
        $audit = & $npm audit --json --audit-level=moderate 2>&1
        $exitCode = $LASTEXITCODE
        $auditText = $audit -join "`n"
        Write-Utf8 (Join-Path $reports 'npm-audit.json') $auditText
        $npmReport = $auditText | ConvertFrom-Json
        if ($npmReport.PSObject.Properties['error'] -or $exitCode -ne 0) { throw 'La auditoría npm falló o detectó hallazgos moderados, altos o críticos.' }
    } finally { Pop-Location }
} finally { Pop-Location }
