param([string]$RegressionReportPath)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$fixture = Join-Path $repo ('artifacts/sql-risk-tests-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($fixture) | Out-Null
$original = Get-Content -LiteralPath (Join-Path $repo 'docs/sql-risk-acceptance.json') -Raw
$policy = $original | ConvertFrom-Json
$now = ([DateTimeOffset]$policy.acceptedAtUtc).AddHours(1)
$targets = foreach ($group in ($policy.findings | Group-Object target)) {
    @{ Target = $group.Name; Type = $group.Group[0].type; Vulnerabilities = @($group.Group | ForEach-Object {
        @{ VulnerabilityID = $_.cve; PkgName = $_.package; InstalledVersion = $_.version; Severity = $_.severity }
    }) }
}
$baseline = @{
    SchemaVersion = 2; CreatedAt = $now.AddMinutes(-1).ToString('o'); ArtifactType = 'container_image'; ArtifactName = $policy.image
    Metadata = @{ RepoDigests = @($policy.image.Replace(':2022-latest@', '@')); ImageConfig = @{ os = 'linux'; architecture = 'amd64' } }
    Results = @($targets)
} | ConvertTo-Json -Depth 20
$checks = [Collections.Generic.List[object]]::new()
function Check([string]$Name, [bool]$Pass, [scriptblock]$Mutate, [string]$ExpectedError = '') {
    $r = $baseline | ConvertFrom-Json
    $p = $original | ConvertFrom-Json
    $options = @{ ReportPath = (Join-Path $fixture 'report.json'); PolicyPath = (Join-Path $fixture 'policy.json'); ScannerExitCode = 1; Image = $p.image; Now = $now; Profile = 'local-evaluation'; DecisionPath = (Join-Path $fixture 'decision.json') }
    if ($Mutate) { & $Mutate $r $p $options }
    [IO.File]::WriteAllText($options.ReportPath, ($r | ConvertTo-Json -Depth 20))
    [IO.File]::WriteAllText($options.PolicyPath, ($p | ConvertTo-Json -Depth 20))
    $failure = $null
    try { & (Join-Path $repo 'scripts/assert-sql-risk.ps1') @options 6>$null } catch { $failure = $_.Exception.Message }
    if (($null -eq $failure) -ne $Pass -or ($ExpectedError -and $failure -notmatch $ExpectedError)) { throw "$Name failed: expectedPass=$Pass, error=$failure" }
    $checks.Add(@{ name = $Name; result = 'passed' })
}
Check 'Exact accepted baseline' $true {}
Check 'Fewer findings remain accepted' $true { param($r) $r.Results[0].Vulnerabilities = @($r.Results[0].Vulnerabilities | Select-Object -Skip 1) }
Check 'All findings removed with scanner success' $true { param($r,$p,$o) foreach ($t in $r.Results) { $t.Vulnerabilities = @() }; $o.ScannerExitCode = 0 }
Check 'Critical escalation rejected' $false { param($r) $r.Results[0].Vulnerabilities[0].Severity = 'CRITICAL' } 'outside acceptance'
Check 'New CVE rejected' $false { param($r) $r.Results[0].Vulnerabilities[0].VulnerabilityID = 'CVE-2099-99999' } 'outside acceptance'
Check 'Different package rejected' $false { param($r) $r.Results[0].Vulnerabilities[0].PkgName = 'unexpected' } 'outside acceptance'
Check 'Different installed version rejected' $false { param($r) $r.Results[0].Vulnerabilities[0].InstalledVersion = '0.0.0' } 'outside acceptance'
Check 'Duplicate finding rejected' $false { param($r) $r.Results[0].Vulnerabilities += $r.Results[0].Vulnerabilities[0] } 'outside acceptance'
Check 'Unknown target rejected' $false { param($r) $r.Results[0].Target = 'unknown' } 'unexpected'
Check 'Missing target rejected' $false { param($r) $r.Results = @($r.Results | Select-Object -Skip 1) } 'incomplete'
Check 'Duplicate target rejected' $false { param($r) $r.Results[0] = $r.Results[1] } 'duplicate'
Check 'Scanner failure rejected' $false { param($r,$p,$o) $o.ScannerExitCode = 2 } 'scanner failed'
Check 'Success with vulnerabilities rejected' $false { param($r,$p,$o) $o.ScannerExitCode = 0 } 'contradicts'
Check 'Failure with empty findings rejected' $false { param($r) foreach ($t in $r.Results) { $t.Vulnerabilities = @() } } 'contradicts'
Check 'Missing results rejected' $false { param($r) $r.PSObject.Properties.Remove('Results') }
Check 'Wrong report artifact rejected' $false { param($r) $r.ArtifactName = 'different' } 'expected image'
Check 'Wrong report digest rejected' $false { param($r) $r.Metadata.RepoDigests = @('different') } 'mismatch'
Check 'Wrong platform rejected' $false { param($r) $r.Metadata.ImageConfig.os = 'windows' } 'mismatch'
Check 'Different requested image rejected' $false { param($r,$p,$o) $o.Image = 'different' } 'digest'
Check 'Production profile rejected' $false { param($r,$p,$o) $o.Profile = 'production' } 'local evaluation'
Check 'Expired acceptance rejected' $false { param($r,$p,$o) $o.Now = ([DateTimeOffset]$p.expiresAtUtc) } 'validity'
Check 'Premature acceptance rejected' $false { param($r,$p,$o) $o.Now = ([DateTimeOffset]$p.acceptedAtUtc).AddSeconds(-1) } 'validity'
Check 'Longer validity rejected' $false { param($r,$p) $p.expiresAtUtc = (([DateTimeOffset]$p.acceptedAtUtc).AddDays(15)).ToString('o') } 'validity'
Check 'Stale scan rejected' $false { param($r) $r.CreatedAt = $now.AddDays(-2).ToString('o') } 'stale'
Check 'Future scan rejected' $false { param($r) $r.CreatedAt = $now.AddHours(1).ToString('o') } 'future'
Check 'Unapproved policy rejected' $false { param($r,$p) $p.status = 'proposed' } 'not valid'
Check 'Missing owner rejected' $false { param($r,$p) $p.owner = '' } 'ownership'
Check 'Invalid policy baseline rejected' $false { param($r,$p) $p.findings = @($p.findings | Select-Object -Skip 1) } 'incomplete'
Check 'Critical policy entry rejected' $false { param($r,$p) $p.findings[0].severity = 'CRITICAL' } 'invalid finding'
Check 'Duplicate policy entry rejected' $false { param($r,$p) $p.findings[0] = $p.findings[1] } 'duplicate'
if ($RegressionReportPath) { [IO.File]::WriteAllText($RegressionReportPath, (@{ checks = $checks.ToArray(); total = $checks.Count } | ConvertTo-Json -Depth 5)) }
Write-Host "SQL acceptance regression checks passed: $($checks.Count)."
