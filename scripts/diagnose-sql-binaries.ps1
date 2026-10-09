. (Join-Path $PSScriptRoot 'common.ps1')
if ($env:GITHUB_ACTIONS -ne 'true' -or $env:OS -eq 'Windows_NT') { throw 'Este diagnóstico se ejecuta únicamente en el runner Linux de CI.' }
$lock = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'toolchain-lock.json') -Raw | ConvertFrom-Json
$toolsDirectory = Join-Path $script:ReleaseRoot '.tools'
$binaryDirectory = Join-Path $toolsDirectory 'sql-binaries'
$reportDirectory = Join-Path $script:ReleaseRoot 'artifacts/security/sql-binaries'
foreach ($directory in @($toolsDirectory, $binaryDirectory, $reportDirectory)) { [IO.Directory]::CreateDirectory($directory) | Out-Null }
$archive = Join-Path $toolsDirectory 'go-linux-amd64.tar.gz'
Invoke-WebRequest -Uri $lock.tools.go.url -OutFile $archive
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $lock.tools.go.sha256) { throw 'Checksum oficial de Go no coincide.' }
Invoke-Checked 'tar' @('-xzf', $archive, '-C', $toolsDirectory)
$go = Join-Path $toolsDirectory 'go/bin/go'
$env:GOROOT = Join-Path $toolsDirectory 'go'
$env:PATH = (Join-Path $toolsDirectory 'go/bin') + [IO.Path]::PathSeparator + $env:PATH
$env:GOTOOLCHAIN = 'local'
$env:GOENV = 'off'
$env:GOPROXY = 'https://proxy.golang.org'
$env:GOSUMDB = 'sum.golang.org'
$env:GONOSUMDB = ''; $env:GOPRIVATE = ''; $env:GOINSECURE = ''
$env:GOPATH = Join-Path $toolsDirectory 'gopath'
$env:GOCACHE = Join-Path $toolsDirectory 'go-cache'
$env:GOBIN = Join-Path $toolsDirectory 'diagnostic-bin'
if ((& $go env GOVERSION).Trim() -ne "go$($lock.tools.go.version)" -or $LASTEXITCODE -ne 0) { throw 'La versión Go no corresponde al pin.' }
Invoke-Checked $go @('install', $lock.tools.govulncheck.module)
$govulncheck = Join-Path $env:GOBIN 'govulncheck'
$toolVersion = & $govulncheck -version
if ($LASTEXITCODE -ne 0) { throw 'No fue posible identificar govulncheck.' }
Write-Utf8 (Join-Path $reportDirectory 'tool-version.txt') ($toolVersion -join "`n")
$image = $lock.images.sqlserver
$container = & docker create --entrypoint /bin/true $image
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($container)) { throw 'No se pudo crear el contenedor detenido de extracción.' }
$container = $container.Trim()
$summary = @()
$failed = $false
try {
    # This container is never started. Only immutable files are copied; none of
    # the vendor ELF files are executed or modified during this diagnosis.
    foreach ($item in @(
        @{ Name = 'launchpadd'; Path = '/opt/mssql/bin/launchpadd' },
        @{ Name = 'launchpad'; Path = '/opt/mssql-extensibility/bin/launchpad' },
        @{ Name = 'setnetbr'; Path = '/opt/mssql/bin/setnetbr' }
    )) {
        $binary = Join-Path $binaryDirectory $item.Name
        Invoke-Checked 'docker' @('cp', "${container}:$($item.Path)", $binary)
        $platform = & file --brief $binary
        if ($LASTEXITCODE -ne 0 -or $platform -notmatch 'ELF 64-bit.*x86-64') { throw 'El archivo extraído no corresponde a ELF Linux x64.' }
        $buildInfo = & $go version -m $binary
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo leer la información de compilación Go.' }
        $metadata = [ordered]@{
            name = $item.Name; image = $image; path = $item.Path
            sha256 = (Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash.ToLowerInvariant()
            platform = [string]$platform; buildInfo = @($buildInfo)
            recordedUtc = [DateTime]::UtcNow.ToString('o'); binaryExecuted = $false
        }
        Write-Utf8 (Join-Path $reportDirectory "$($item.Name).metadata.json") ($metadata | ConvertTo-Json -Depth 5)
        foreach ($level in @('module', 'symbol')) {
            $report = Join-Path $reportDirectory "$($item.Name).$level.json"
            $errors = Join-Path $reportDirectory "$($item.Name).$level.stderr.txt"
            & $govulncheck -mode=binary "-scan=$level" -format=json $binary 1> $report 2> $errors
            $scanExit = $LASTEXITCODE
            $hasOutput = (Test-Path -LiteralPath $report) -and (Get-Item -LiteralPath $report).Length -gt 0
            if ($scanExit -ne 0 -or -not $hasOutput) { $failed = $true }
            $summary += [pscustomobject]@{ binary = $item.Name; scan = $level; exitCode = $scanExit; hasOutput = $hasOutput; status = 'diagnostic-only-requires-record-review' }
        }
    }
} finally {
    Invoke-Checked 'docker' @('rm', '-v', $container)
}
Write-Utf8 (Join-Path $reportDirectory 'summary.json') ($summary | ConvertTo-Json -Depth 5)
Write-Utf8 (Join-Path $reportDirectory 'README.txt') 'Govulncheck JSON is a stream of records. Exit 0 does not mean zero vulnerabilities. Review config/feed metadata, SBOM, findings, symbols and stderr. Symbol presence is not a full call graph or proof of reachability. No output here creates a VEX exception or authorizes the ZIP. Vendor binaries were copied from a never-started container and were not executed. The required Trivy SQL image gate remains unchanged.'
if ($failed) { throw 'El diagnóstico binario no pudo completarse; no interprete informes parciales como ausencia de vulnerabilidades.' }
Write-Host 'Diagnóstico binario guardado para revisión. No modifica el resultado del gate de imagen SQL.'
