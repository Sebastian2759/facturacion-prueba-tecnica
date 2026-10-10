Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:ReleaseRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))

function Invoke-Checked {
    param([string]$File, [string[]]$Arguments)
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$File terminó con código $LASTEXITCODE." }
}

function Assert-Docker {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { throw 'Instale Docker Desktop y habilite contenedores Linux. Consulte LEEME.html; puede requerir virtualización, WSL 2 y reiniciar Windows.' }
    $os = & docker info --format '{{.OSType}}' 2>$null
    if ($LASTEXITCODE -ne 0 -or $os -ne 'linux') { throw 'Abra Docker Desktop y active el motor Linux con WSL 2. Si indica virtualización deshabilitada, revise Administrador de tareas > Rendimiento > CPU; habilite SVM (AMD) o VT-x (Intel) en BIOS según el fabricante y reinicie. Consulte LEEME.html. No es necesario cambiar BIOS si Docker ya funciona.' }
    & docker compose version | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Se requiere Docker Compose v2 incluido en Docker Desktop.' }
}

function Invoke-Compose {
    param([string[]]$Arguments)
    $compose = @('compose', '--env-file', (Join-Path $script:ReleaseRoot 'local.env'), '-f', (Join-Path $script:ReleaseRoot 'compose.yaml'))
    Invoke-Checked 'docker' ($compose + $Arguments)
}

function New-LocalSecret {
    $bytes = New-Object byte[] 32
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generator.GetBytes($bytes) } finally { $generator.Dispose() }
    return 'Aa1!' + ([BitConverter]::ToString($bytes).Replace('-', '').ToLowerInvariant())
}

function Write-Utf8 {
    param([string]$Path, [string]$Text)
    [IO.File]::WriteAllText($Path, $Text, (New-Object Text.UTF8Encoding $false))
}

function Protect-LocalFile {
    param([string]$Path)
    if ($env:OS -eq 'Windows_NT') {
        $identity = [Security.Principal.WindowsIdentity]::GetCurrent().Name
        $acl = Get-Acl -LiteralPath $Path
        $acl.SetAccessRuleProtection($true, $false)
        $rule = New-Object Security.AccessControl.FileSystemAccessRule($identity, 'FullControl', 'Allow')
        $acl.AddAccessRule($rule)
        Set-Acl -LiteralPath $Path -AclObject $acl
    } else {
        Invoke-Checked 'chmod' @('600', $Path)
    }
}

function Initialize-LocalSettings {
    $path = Join-Path $script:ReleaseRoot 'local.env'
    if (Test-Path -LiteralPath $path) { return }
    $installation = [Guid]::NewGuid().ToString()
    $text = @(
        "COMPOSE_PROJECT_NAME=facturacion_$($installation.Replace('-', '').Substring(0,12))"
        "INSTALLATION_ID=$installation"
        'DATABASE_NAME=FacturacionLocal'
        "SQL_ADMIN_PASSWORD=$(New-LocalSecret)"
        "SQL_RUNTIME_PASSWORD=$(New-LocalSecret)"
    ) -join "`n"
    Write-Utf8 $path ($text + "`n")
    Protect-LocalFile $path
}

function Get-DatabaseContainer {
    $container = Invoke-Compose @('ps', '-q', 'database')
    if ([string]::IsNullOrWhiteSpace($container)) { throw 'La base de datos no está iniciada. Ejecute iniciar.cmd.' }
    return ($container | Select-Object -Last 1).Trim()
}

function Invoke-ContainerSql {
    param([string]$Container, [string]$Sql)
    # POSIX assignment expansion does not split the password. Single quotes around
    # the delimiter also survive Windows PowerShell 5.1 native argument passing.
    $Sql | & docker exec -i $Container sh -c 'SQLCMDPASSWORD=$MSSQL_SA_PASSWORD /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -r1 -h -1 -W -s ''|'' -i /dev/stdin'
    if ($LASTEXITCODE -ne 0) { throw 'La operación SQL falló. Los datos existentes se conservan.' }
}

function Get-LocalDatabaseName {
    $line = Get-Content -LiteralPath (Join-Path $script:ReleaseRoot 'local.env') | Where-Object { $_ -match '^DATABASE_NAME=' } | Select-Object -First 1
    $name = $line.Substring('DATABASE_NAME='.Length)
    if ($name -notmatch '^Facturacion(Local|CI)(_[A-Za-z0-9_]+)?$') { throw 'Nombre de base local no permitido.' }
    return $name
}
