param([string]$Root = (Join-Path $PSScriptRoot '..'))
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# Windows PowerShell 5.1 can autoload the Utility cmdlets without its script
# functions. Import the complete module before using Get-FileHash.
Import-Module Microsoft.PowerShell.Utility -Force -ErrorAction Stop
$rootPath = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
$sumPath = Join-Path $rootPath 'SHA256SUMS.txt'
if (-not (Test-Path -LiteralPath $sumPath)) { throw 'Este directorio no contiene un paquete de evaluación generado. Descargue el ZIP de release completo.' }
$lines = Get-Content -LiteralPath $sumPath
if ($lines.Count -lt 5) { throw 'Manifiesto de integridad incompleto.' }
foreach ($line in $lines) {
    if ($line -notmatch '^([a-fA-F0-9]{64})  (.+)$') { throw 'Formato de checksums no válido.' }
    $hash = $Matches[1]
    $relative = $Matches[2]
    if ([IO.Path]::IsPathRooted($relative) -or $relative -match '(^|[\\/])\.\.([\\/]|$)') { throw 'Ruta insegura en el manifiesto.' }
    $path = [IO.Path]::GetFullPath((Join-Path $rootPath $relative))
    if (-not $path.StartsWith($rootPath, [StringComparison]::OrdinalIgnoreCase)) { throw 'Ruta fuera de la entrega.' }
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Falta el archivo: $relative" }
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $hash) { throw "Integridad no válida: $relative. Vuelva a descargar el ZIP." }
}
$release = Get-Content -LiteralPath (Join-Path $rootPath 'release.json') -Raw | ConvertFrom-Json
if ($release.status -notin @('release-verified', 'release-verified-with-accepted-risk')) { throw 'El paquete no ha superado los controles de entrega.' }
if ($release.status -eq 'release-verified-with-accepted-risk') {
    if ($release.profile -ne 'local-evaluation' -or $release.sqlRiskAcceptance.acceptanceId -ne 'SQL-LOCAL-2026-10-09' -or
        $release.sqlRiskAcceptance.status -notin @('accepted-risk', 'passed-no-findings') -or $release.sqlRiskAcceptance.image -ne $release.images.sqlserver) { throw 'La aceptación SQL del paquete no es válida.' }
    Write-Host 'Evaluación local con riesgo SQL aceptado. Consulte docs/sql-risk-acceptance.md; utilice datos ficticios.'
}
Write-Host 'Integridad de los archivos de entrega verificada.'
