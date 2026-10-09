param([string]$ReportPath)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Run these checks on Windows; they verify NTFS permissions and Windows PowerShell compatibility.' }
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$fixture = Join-Path $repo ('artifacts/windows delivery tests/' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($fixture) | Out-Null
$results = New-Object 'System.Collections.Generic.List[object]'
function Pass([string]$Name) { $results.Add([pscustomobject]@{ check = $Name; result = 'passed' }) }
function Expect-Rejection([string]$Name, [scriptblock]$Action, [string]$MessagePattern) {
    $failure = $null
    try { & $Action | Out-Null } catch { $failure = $_.Exception.Message }
    if (-not $failure -or $failure -notmatch $MessagePattern) { throw "Expected rejection did not occur: $Name" }
    Pass $Name
}
foreach ($name in @('common.ps1', 'start.ps1', 'stop.ps1', 'backup.ps1', 'restore.ps1', 'diagnose.ps1', 'verify-release.ps1')) {
    $source = Join-Path (Join-Path $repo 'scripts') $name
    $text = [IO.File]::ReadAllText($source, [Text.Encoding]::UTF8)
    $tokens = $null; $errors = $null
    [Management.Automation.Language.Parser]::ParseInput($text, [ref]$tokens, [ref]$errors) | Out-Null
    if ($errors.Count) { throw "PowerShell syntax errors: $name" }
    # The package writer applies this same encoding for Windows PowerShell 5.1.
    [IO.File]::WriteAllText((Join-Path $fixture $name), $text, (New-Object Text.UTF8Encoding $true))
}
Pass 'Evaluator scripts parse in the actual PowerShell engine on Windows'
. (Join-Path $fixture 'common.ps1')
$script:ReleaseRoot = $fixture
$settings = Join-Path $fixture 'local.env'
try {
    Initialize-LocalSettings
    $content = [IO.File]::ReadAllText($settings)
    $admin = [regex]::Match($content, '(?m)^SQL_ADMIN_PASSWORD=(Aa1![0-9a-f]{64})$')
    $runtime = [regex]::Match($content, '(?m)^SQL_RUNTIME_PASSWORD=(Aa1![0-9a-f]{64})$')
    if (-not $admin.Success -or -not $runtime.Success -or $admin.Groups[1].Value -eq $runtime.Groups[1].Value) { throw 'Independent random secrets were not generated.' }
    Pass 'Distinct random database credentials generated without disclosure'
    $installation = [regex]::Match($content, '(?m)^INSTALLATION_ID=([0-9a-f-]{36})$')
    if (-not $installation.Success -or [Guid]::Parse($installation.Groups[1].Value) -eq [Guid]::Empty) { throw 'Invalid installation identifier.' }
    Pass 'Valid installation identifier generated'
    $currentSid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
    $acl = Get-Acl -LiteralPath $settings
    $rules = @($acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier]))
    if (-not $acl.AreAccessRulesProtected -or $rules.Count -ne 1 -or $rules[0].IsInherited -or $rules[0].IdentityReference.Value -ne $currentSid -or $rules[0].AccessControlType -ne 'Allow' -or $rules[0].FileSystemRights -ne 'FullControl') { throw 'Settings permissions are not restricted to the current user.' }
    Pass 'NTFS ACL restricted to current user with inheritance disabled'
    Initialize-LocalSettings
    if ([IO.File]::ReadAllText($settings) -cne $content) { throw 'Repeated initialization changed credentials or ownership.' }
    Pass 'Repeated initialization preserves settings and ownership'
} finally {
    if (Test-Path -LiteralPath $settings) { Remove-Item -LiteralPath $settings }
    $content = $null; $admin = $null; $runtime = $null
}
$package = Join-Path $fixture 'synthetic package with spaces'
[IO.Directory]::CreateDirectory($package) | Out-Null
$verify = Join-Path $fixture 'verify-release.ps1'
Expect-Rejection 'Reject missing checksums' { & $verify -Root $package 6>$null } 'paquete de evaluaci'
$files = @('release.json', 'README.md', 'first.txt', 'second.txt', 'third.txt')
foreach ($name in $files) { Write-Utf8 (Join-Path $package $name) 'Synthetic fixture, not an application release.' }
Write-Utf8 (Join-Path $package 'release.json') '{"status":"release-verified","fixture":true}'
function Write-FixtureChecksums {
    $lines = foreach ($name in $files) { '{0}  {1}' -f (Get-FileHash -LiteralPath (Join-Path $package $name) -Algorithm SHA256).Hash.ToLowerInvariant(), $name }
    Write-Utf8 (Join-Path $package 'SHA256SUMS.txt') (($lines -join "`n") + "`n")
}
Write-FixtureChecksums
& $verify -Root $package 6>$null
Pass 'Verify intact synthetic package in a path with spaces'
Write-Utf8 (Join-Path $package 'first.txt') 'Changed after checksum creation'
Expect-Rejection 'Reject modified file' { & $verify -Root $package 6>$null } 'Integridad no v'
Write-FixtureChecksums
Remove-Item -LiteralPath (Join-Path $package 'third.txt')
Expect-Rejection 'Reject missing file' { & $verify -Root $package 6>$null } 'Falta el archivo'
Write-Utf8 (Join-Path $package 'third.txt') 'Synthetic fixture restored'
Write-Utf8 (Join-Path $package 'release.json') '{"status":"prepared-not-verified","fixture":true}'
Write-FixtureChecksums
Expect-Rejection 'Reject unapproved package status' { & $verify -Root $package 6>$null } 'controles de entrega'
$sumPath = Join-Path $package 'SHA256SUMS.txt'
$valid = [IO.File]::ReadAllText($sumPath)
foreach ($unsafePath in @('../outside.txt', '..\outside.txt', 'C:\outside.txt')) {
    Write-Utf8 $sumPath (($valid -replace '  first.txt', ('  ' + $unsafePath)))
    Expect-Rejection ('Reject unsafe path: ' + $unsafePath) { & $verify -Root $package 6>$null } 'Ruta insegura'
}
# These fixtures exercise verifier rejection; never label them as deliverables.
Write-Utf8 $sumPath $valid
$report = [ordered]@{
    status = 'windows-script-checks-passed-not-an-installation-test'
    verifiedUtc = [DateTime]::UtcNow.ToString('o')
    powerShellVersion = $PSVersionTable.PSVersion.ToString()
    sourceCommit = (& git -C $repo rev-parse HEAD).Trim()
    testScriptSha256 = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
    checks = @($results.ToArray())
    exclusions = @('Docker/WSL installation', 'Container startup on Windows', 'Real Novasoft connectivity', 'Final application ZIP approval')
}
if ($ReportPath) { Write-Utf8 ([IO.Path]::GetFullPath($ReportPath)) ($report | ConvertTo-Json -Depth 6) }
Write-Host "Windows delivery script checks passed: $($results.Count). Synthetic package fixtures; no Docker installation or application startup performed."
