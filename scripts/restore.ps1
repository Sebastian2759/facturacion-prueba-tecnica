param([string]$BackupPath, [string]$NewDatabaseName = ('FacturacionLocal_Restored_' + (Get-Date).ToString('yyyyMMddHHmmss')))
. (Join-Path $PSScriptRoot 'common.ps1')
Assert-Docker
if ([string]::IsNullOrWhiteSpace($BackupPath)) { $BackupPath = Read-Host 'Ruta completa del archivo .bak que desea restaurar' }
if ($NewDatabaseName -notmatch '^FacturacionLocal_Restored_[A-Za-z0-9_]{1,39}$') { throw 'Use un nombre FacturacionLocal_Restored_ seguido de 1 a 39 letras, números o guiones bajos.' }
$backup = Get-Item -LiteralPath $BackupPath
if ($backup.Extension -ne '.bak' -or $backup.PSIsContainer) { throw 'Seleccione un archivo .bak.' }
$container = Get-DatabaseContainer
Invoke-Checked 'docker' @('exec', $container, 'mkdir', '-p', '/var/opt/mssql/backup')
Invoke-Checked 'docker' @('cp', $backup.FullName, "${container}:/var/opt/mssql/backup/restore-input.bak")
$files = Invoke-ContainerSql $container "RESTORE FILELISTONLY FROM DISK=N'/var/opt/mssql/backup/restore-input.bak';"
$moves = @()
$index = 0
foreach ($line in $files) {
    $parts = $line.Split('|')
    if ($parts.Length -gt 2 -and $parts[2].Trim() -in @('D', 'L')) {
        $logical = $parts[0].Trim()
        if ($logical -notmatch '^[A-Za-z0-9_]+$') { throw 'El backup contiene nombres lógicos no admitidos por la guía automática.' }
        $extension = if ($parts[2].Trim() -eq 'L') { 'ldf' } else { 'mdf' }
        $moves += "MOVE N'$logical' TO N'/var/opt/mssql/data/${NewDatabaseName}_${index}.$extension'"
        $index++
    }
}
if ($moves.Count -lt 2) { throw 'No fue posible validar los archivos del backup.' }
$sql = @"
IF DB_ID(N'$NewDatabaseName') IS NOT NULL THROW 51000, 'La base destino ya existe; no se sobrescribirá.', 1;
RESTORE DATABASE [$NewDatabaseName] FROM DISK=N'/var/opt/mssql/backup/restore-input.bak' WITH CHECKSUM, $($moves -join ', ');
GO
USE [$NewDatabaseName];
IF USER_ID(N'facturacion_runtime') IS NOT NULL ALTER USER [facturacion_runtime] WITH LOGIN=[facturacion_runtime];
"@
Invoke-ContainerSql $container $sql | Out-Null
Write-Host "Restauración completada en la nueva base $NewDatabaseName. La aplicación sigue usando su base anterior."
Write-Host 'Para utilizar la copia: haga primero backup; detenga la app; cambie únicamente DATABASE_NAME en local.env por el nombre mostrado y ejecute iniciar.cmd.'
