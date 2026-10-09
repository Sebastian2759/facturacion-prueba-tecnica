. (Join-Path $PSScriptRoot 'common.ps1')
$lock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'toolchain-lock.json') -Raw | ConvertFrom-Json
$reports = Join-Path $script:ReleaseRoot 'artifacts/security'
[IO.Directory]::CreateDirectory($reports) | Out-Null
$failed = $false
$summary = @()
# Candidate results are diagnostic only. The actual SQL 2022 image remains a
# required gate and no candidate silently replaces the configured database.
$targets = @(
    @{ Name = 'aspnet'; Image = $lock.images.aspnet; Required = $true },
    @{ Name = 'sqlserver'; Image = $lock.images.sqlserver; Required = $true },
    @{ Name = 'sql2025-candidate'; Image = $lock.diagnosticImages.sql2025Candidate; Required = $false }
)
foreach ($target in $targets) {
    $path = Join-Path $reports ("trivy-" + $target.Name + '.json')
    & trivy image --scanners vuln --severity HIGH,CRITICAL --exit-code 1 --format json --output $path $target.Image
    $scanExit = $LASTEXITCODE
    $findings = 0
    $validReport = $false
    if (Test-Path -LiteralPath $path) {
        try {
            $report = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
            if ($report.PSObject.Properties['SchemaVersion'] -and $report.PSObject.Properties['Results']) {
                $validReport = $true
                foreach ($result in $report.Results) {
                    if ($result.PSObject.Properties['Vulnerabilities']) { $findings += @($result.Vulnerabilities | Where-Object { $null -ne $_ }).Count }
                }
            }
        } catch { $validReport = $false }
    }
    if ($target.Required -and ($scanExit -ne 0 -or -not $validReport -or $findings -gt 0)) { $failed = $true }
    $summary += [pscustomobject]@{ name = $target.Name; image = $target.Image; required = $target.Required; exitCode = $scanExit; validReport = $validReport; highCriticalFindings = $findings }
    Write-Host "$($target.Name): HIGH/CRITICAL=$findings; exit=$scanExit; required=$($target.Required); report=$validReport"
}
Write-Utf8 (Join-Path $reports 'runtime-images-summary.json') ($summary | ConvertTo-Json -Depth 5)
if ($failed) { throw 'Una imagen requerida tiene hallazgos o no pudo verificarse. El ZIP permanece bloqueado; consulte los reportes JSON. El candidato no autoriza un cambio de motor.' }
