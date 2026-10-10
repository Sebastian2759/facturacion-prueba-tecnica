param(
    [Parameter(Mandatory=$true)][string]$ReportPath,
    [Parameter(Mandatory=$true)][int]$ScannerExitCode,
    [Parameter(Mandatory=$true)][string]$Image,
    [string]$PolicyPath = (Join-Path $PSScriptRoot '../docs/sql-risk-acceptance.json'),
    [string]$Profile = 'local-evaluation',
    [string]$DecisionPath,
    [DateTimeOffset]$Now = [DateTimeOffset]::UtcNow
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# Never suppress scanner failures, new findings, critical severity or image drift.
if ($ScannerExitCode -notin @(0, 1)) { throw 'SQL scanner failed.' }
$policy = Get-Content -LiteralPath $PolicyPath -Raw | ConvertFrom-Json
$report = Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json
if ($policy.schemaVersion -ne 1 -or $policy.status -cne 'accepted' -or $policy.id -cne 'SQL-LOCAL-2026-10-09') { throw 'SQL acceptance is not valid.' }
if ($Profile -cne 'local-evaluation' -or $policy.scope -cne $Profile) { throw 'SQL acceptance is limited to local evaluation.' }
if ($Image -cne $policy.image -or $Image -notmatch '@sha256:[a-f0-9]{64}$') { throw 'SQL image differs from the accepted digest.' }
$start = ([DateTimeOffset]$policy.acceptedAtUtc)
$expiry = ([DateTimeOffset]$policy.expiresAtUtc)
if ($expiry -le $start -or ($expiry - $start).TotalDays -gt 14 -or $Now -lt $start -or $Now -ge $expiry) { throw 'SQL acceptance is outside its validity period.' }
if ([string]::IsNullOrWhiteSpace($policy.owner) -or [string]::IsNullOrWhiteSpace($policy.authorization) -or [string]::IsNullOrWhiteSpace($policy.reason)) { throw 'SQL acceptance lacks ownership or rationale.' }
if ($report.SchemaVersion -ne 2 -or $report.ArtifactType -cne 'container_image' -or $report.ArtifactName -cne $Image) { throw 'SQL report does not identify the expected image.' }
$digestReference = $Image.Replace(':2022-latest@', '@')
if ($report.Metadata.RepoDigests -cnotcontains $digestReference -or $report.Metadata.ImageConfig.os -cne 'linux' -or $report.Metadata.ImageConfig.architecture -cne 'amd64') { throw 'SQL report digest or platform mismatch.' }
$created = ([DateTimeOffset]$report.CreatedAt)
if ($created -gt $Now.AddMinutes(5) -or $created -lt $Now.AddHours(-24)) { throw 'SQL report is stale or dated in the future.' }
if (-not $report.PSObject.Properties['Results'] -or @($report.Results).Count -ne 4) { throw 'SQL report results are incomplete.' }
function Get-Key($Target, $Type, $Cve, $Package, $Version, $Severity) {
    return (@($Target, $Type, $Cve, $Package, $Version, $Severity) | ConvertTo-Json -Compress)
}
$allowed = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$expectedTargets = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($entry in $policy.findings) {
    if ($entry.severity -cne 'HIGH' -or $entry.cve -notmatch '^CVE-\d{4}-\d+$') { throw 'SQL policy contains an invalid finding.' }
    if (-not $allowed.Add((Get-Key $entry.target $entry.type $entry.cve $entry.package $entry.version $entry.severity))) { throw 'SQL policy contains duplicate findings.' }
    [void]$expectedTargets.Add((Get-Key $entry.target $entry.type '' '' '' ''))
}
if ($allowed.Count -ne 71 -or @($policy.findings.cve | Sort-Object -Unique).Count -ne 24 -or $expectedTargets.Count -ne 4) { throw 'SQL policy baseline is incomplete.' }
$seenTargets = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$seenFindings = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($result in $report.Results) {
    $targetKey = Get-Key $result.Target $result.Type '' '' '' ''
    if (-not $expectedTargets.Contains($targetKey) -or -not $seenTargets.Add($targetKey)) { throw 'SQL report has an unexpected or duplicate target.' }
    if (-not $result.PSObject.Properties['Vulnerabilities']) { continue }
    foreach ($finding in $result.Vulnerabilities) {
        if ($null -eq $finding) { throw 'SQL report contains an invalid finding.' }
        $key = Get-Key $result.Target $result.Type $finding.VulnerabilityID $finding.PkgName $finding.InstalledVersion $finding.Severity
        if (-not $allowed.Contains($key) -or -not $seenFindings.Add($key)) { throw "SQL finding is outside acceptance: $($finding.VulnerabilityID) / $($result.Target)." }
    }
}
if (($seenFindings.Count -gt 0 -and $ScannerExitCode -ne 1) -or ($seenFindings.Count -eq 0 -and $ScannerExitCode -ne 0)) { throw 'SQL scanner exit code contradicts its report.' }
$decision = [ordered]@{
    status = $(if ($seenFindings.Count) { 'accepted-risk' } else { 'passed-no-findings' })
    acceptanceId = $policy.id; owner = $policy.owner; profile = $Profile; image = $Image
    expiresAtUtc = $policy.expiresAtUtc; highFindings = $seenFindings.Count; criticalFindings = 0
    reportSha256 = (Get-FileHash -LiteralPath $ReportPath -Algorithm SHA256).Hash.ToLowerInvariant()
    policySha256 = (Get-FileHash -LiteralPath $PolicyPath -Algorithm SHA256).Hash.ToLowerInvariant()
    evaluatedAtUtc = $Now.ToUniversalTime().ToString('o'); runId = $env:GITHUB_RUN_ID; sourceCommit = $env:GITHUB_SHA
}
if ($DecisionPath) { [IO.File]::WriteAllText($DecisionPath, ($decision | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false)) }
Write-Host "SQL gate: $($decision.status), HIGH=$($decision.highFindings), acceptance=$($policy.id). Findings remain in the original report."
