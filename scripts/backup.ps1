. (Join-Path $PSScriptRoot 'common.ps1')
Assert-Docker
$container = Get-DatabaseContainer
$database = Get-LocalDatabaseName
$backupDirectory = Join-Path $script:ReleaseRoot 'backups'
[IO.Directory]::CreateDirectory($backupDirectory) | Out-Null
$fileName = "backup-$((Get-Date).ToString('yyyyMMdd-HHmmss')).bak"
Invoke-Checked 'docker' @('exec', $container, 'mkdir', '-p', '/var/opt/mssql/backup')
Invoke-ContainerSql $container "BACKUP DATABASE [$database] TO DISK=N'/var/opt/mssql/backup/$fileName' WITH CHECKSUM; RESTORE VERIFYONLY FROM DISK=N'/var/opt/mssql/backup/$fileName' WITH CHECKSUM;" | Out-Null
$path = Join-Path $backupDirectory $fileName
Invoke-Checked 'docker' @('cp', "${container}:/var/opt/mssql/backup/$fileName", $path)
Protect-LocalFile $path
Write-Host "Backup verificado: $path"
Write-Host 'Contiene sus datos de evaluación; no lo publique junto con el ZIP original.'
