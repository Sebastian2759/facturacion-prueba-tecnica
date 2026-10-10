param([string]$ReportPath)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module Microsoft.PowerShell.Utility -Force -ErrorAction Stop
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
foreach ($name in @('common.ps1', 'start.ps1', 'stop.ps1', 'backup.ps1', 'restore.ps1', 'diagnose.ps1', 'verify-release.ps1', 'configure-novasoft.ps1')) {
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
# Reproduce the actual start -> common -> verifier script scopes in a fresh
# Windows PowerShell process; importing Utility in this test runner hid the bug.
$nestedProbe = Join-Path $fixture 'nested-start.ps1'
$probeText = @'
param([string]$Common, [string]$Verifier, [string]$Package)
. $Common
& $Verifier -Root $Package
'@
Write-Utf8 $nestedProbe $probeText
& powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File $nestedProbe -Common (Join-Path $fixture 'common.ps1') -Verifier $verify -Package $package
if ($LASTEXITCODE -ne 0) { throw 'Nested Windows PowerShell verifier failed.' }
Pass 'Fresh Windows PowerShell common/verifier script scopes verify the intact package'
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
$accepted = @{ status = 'release-verified-with-accepted-risk'; profile = 'local-evaluation'; images = @{ sqlserver = 'synthetic-image' }; sqlRiskAcceptance = @{ acceptanceId = 'SQL-LOCAL-2026-10-09'; status = 'accepted-risk'; image = 'synthetic-image' } }
Write-Utf8 (Join-Path $package 'release.json') ($accepted | ConvertTo-Json -Depth 5)
Write-FixtureChecksums
& $verify -Root $package 6>$null
Pass 'Verify synthetic local package with explicit risk acceptance'
$accepted.profile = 'production'
Write-Utf8 (Join-Path $package 'release.json') ($accepted | ConvertTo-Json -Depth 5)
Write-FixtureChecksums
Expect-Rejection 'Reject SQL acceptance in production package' { & $verify -Root $package 6>$null } 'aceptaci'
$accepted.profile = 'local-evaluation'
$accepted.sqlRiskAcceptance.image = 'different-image'
Write-Utf8 (Join-Path $package 'release.json') ($accepted | ConvertTo-Json -Depth 5)
Write-FixtureChecksums
Expect-Rejection 'Reject SQL acceptance for different package image' { & $verify -Root $package 6>$null } 'aceptaci'
$accepted.sqlRiskAcceptance.image = 'synthetic-image'
$accepted.status = 'local-verified-with-accepted-risk'
$accepted.sourceCommit = 'fixture-commit'
$accepted.version = '0.0.0-local'
$accepted.backupVerified = $true
Write-Utf8 (Join-Path $package 'release.json') ($accepted | ConvertTo-Json -Depth 5)
Write-FixtureChecksums
Expect-Rejection 'Reject local package without receipt' { & $verify -Root $package 6>$null } 'Falta evidencia'
[IO.Directory]::CreateDirectory((Join-Path $package 'docs')) | Out-Null
$files += 'docs/local-verification.json'
$receipt = @{ environment = 'Windows Docker Desktop'; status = 'passed-with-accepted-sql-risk'; sourceCommit = 'different'; version = $accepted.version }
Write-Utf8 (Join-Path $package 'docs/local-verification.json') ($receipt | ConvertTo-Json)
Write-FixtureChecksums
Expect-Rejection 'Reject local receipt for another commit' { & $verify -Root $package 6>$null } 'no corresponde'
$receipt.sourceCommit = $accepted.sourceCommit
Write-Utf8 (Join-Path $package 'docs/local-verification.json') ($receipt | ConvertTo-Json)
Write-FixtureChecksums
& $verify -Root $package 6>$null
Pass 'Verify local receipt bound to version and commit'
# Execute the real CMD wrappers against synthetic scripts: no Docker or data changes.
$launcherRoot = Join-Path $fixture 'launcher fixtures with spaces'
[IO.Directory]::CreateDirectory((Join-Path $launcherRoot 'scripts')) | Out-Null
foreach ($entry in @(@('iniciar', 'start'), @('detener', 'stop'), @('backup', 'backup'), @('diagnosticar', 'diagnose'), @('restaurar', 'restore'))) {
    $launcher = Join-Path $launcherRoot ($entry[0] + '.cmd')
    Copy-Item -LiteralPath (Join-Path $repo ($entry[0] + '.cmd')) -Destination $launcher
    foreach ($expectedExit in @(0, 7)) {
        $body = 'param([switch]$NoBrowser, [string]$BackupPath)' + "`r`n"
        if ($entry[0] -eq 'iniciar') { $body += 'if (-not $NoBrowser) { exit 91 }' + "`r`n" }
        if ($entry[0] -eq 'restaurar') { $body += 'if ($BackupPath -ne "backup with spaces.bak") { exit 92 }' + "`r`n" }
        $body += "exit $expectedExit"
        Write-Utf8 (Join-Path $launcherRoot ('scripts/' + $entry[1] + '.ps1')) $body
        $arguments = ''
        if ($entry[0] -eq 'iniciar') { $arguments = ' -NoBrowser' }
        if ($entry[0] -eq 'restaurar') { $arguments = ' -BackupPath "backup with spaces.bak"' }
        $info = New-Object Diagnostics.ProcessStartInfo
        $info.FileName = $env:ComSpec
        $info.Arguments = '/d /c ""' + $launcher + '"' + $arguments + '"'
        $info.UseShellExecute = $false
        $info.CreateNoWindow = $true
        $info.RedirectStandardInput = $true
        $info.RedirectStandardOutput = $true
        $info.RedirectStandardError = $true
        $process = [Diagnostics.Process]::Start($info)
        try {
            $process.StandardInput.WriteLine('')
            $process.StandardInput.Close()
            $output = $process.StandardOutput.ReadToEnd()
            $errorOutput = $process.StandardError.ReadToEnd()
            $process.WaitForExit()
            if ($process.ExitCode -ne $expectedExit) { throw "Launcher $($entry[0]) returned $($process.ExitCode), expected $expectedExit. $errorOutput $output" }
        } finally { $process.Dispose() }
        Pass "CMD $($entry[0]) preserves exit $expectedExit and supported arguments"
    }
}
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
