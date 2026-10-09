using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

const string RuntimeLogin = "facturacion_runtime";
if (args.Length != 1 || args[0] is not ("init" or "migrate"))
{
    Console.Error.WriteLine("Uso: DatabaseMigrator init|migrate");
    Console.Error.WriteLine("Variables: Database__AdminConnectionString, Database__Name y Database__RuntimePassword (init).");
    return 2;
}

try
{
    var operation = args[0];
    var name = Environment.GetEnvironmentVariable("Database__Name") ?? string.Empty;
    if (!Regex.IsMatch(name, @"\AFacturacion(?:Local|CI)(?:_[A-Za-z0-9_]{1,48})?\z",
        RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)))
        throw new InvalidOperationException("Database__Name no es un destino local/de CI permitido.");

    var configured = Environment.GetEnvironmentVariable("Database__AdminConnectionString");
    if (string.IsNullOrWhiteSpace(configured))
        throw new InvalidOperationException("Falta la conexión administrativa del migrador.");

    var password = Environment.GetEnvironmentVariable("Database__RuntimePassword");
    if (operation == "init" && (string.IsNullOrEmpty(password) || password.Length is < 32 or > 128))
        throw new InvalidOperationException("La contraseña runtime debe tener entre 32 y 128 caracteres.");

    var adminOptions = new SqlConnectionStringBuilder(configured) { InitialCatalog = "master" };
    await using var admin = new SqlConnection(adminOptions.ConnectionString);
    await admin.OpenAsync();
    await using var existsCommand = new SqlCommand("SELECT DB_ID(@name);", admin);
    existsCommand.Parameters.AddWithValue("@name", name);
    var exists = await existsCommand.ExecuteScalarAsync() is not (null or DBNull);
    if (!exists && operation == "migrate")
        throw new InvalidOperationException("migrate requiere una base existente; use init para crear una nueva.");

    if (!exists)
    {
        // The identifier is restricted to the fixed local/CI prefix and ASCII
        // alphanumerics above; no arbitrary database names reach this statement.
        await using var create = new SqlCommand($"CREATE DATABASE [{name}];", admin) { CommandTimeout = 120 };
        await create.ExecuteNonQueryAsync();
    }

    var databaseOptions = new SqlConnectionStringBuilder(configured) { InitialCatalog = name };
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer(databaseOptions.ConnectionString).Options;
    await using var context = new AppDbContext(options);
    var uninitialized = !exists || await IsUninitializedAsync(context);
    if (exists && !uninitialized && operation == "init" &&
        (await context.Database.GetPendingMigrationsAsync()).Any())
        throw new InvalidOperationException("La base existente requiere migrate explícito antes de iniciar esta versión.");

    if (uninitialized || operation == "migrate")
        await context.Database.MigrateAsync();

    if (operation == "init")
    {
        await EnsureRuntimeAccessAsync(admin, databaseOptions, password!);
        Console.WriteLine("Inicialización completada. Esquema migrado, datos existentes conservados y usuario runtime sólo SELECT/INSERT.");
        Console.WriteLine("No se cargan facturas de ejemplo: los ejemplos públicos son fixtures; la base inicial está vacía.");
    }
    else
    {
        Console.WriteLine("Migraciones aplicadas a la base existente. No se han modificado usuarios ni cargado datos.");
    }
    return 0;
}
catch (SqlException exception)
{
    // SqlException.Message can contain connection/server identifiers; return a
    // diagnostic number, never credentials, connection strings or provider text.
    Console.Error.WriteLine($"La operación SQL falló (código {exception.Number}). Revise acceso, disponibilidad y permisos del proceso de mantenimiento.");
    return 1;
}
catch (Exception exception) when (exception is not OutOfMemoryException)
{
    Console.Error.WriteLine($"No se completó la operación del migrador ({exception.GetType().Name}). Compruebe comando, destino permitido, configuración y estado de migraciones.");
    return 1;
}

static async Task<bool> IsUninitializedAsync(AppDbContext context)
{
    // An interrupted first init may leave an empty database behind. Retrying may
    // complete that first schema, but never upgrades a database with application tables.
    var connection = context.Database.GetDbConnection();
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0 AND [name] <> N'__EFMigrationsHistory';";
    return await command.ExecuteScalarAsync() is int count && count == 0;
}

static async Task EnsureRuntimeAccessAsync(
    SqlConnection admin, SqlConnectionStringBuilder databaseOptions, string password)
{
    // Parameterize the password; QUOTENAME escapes it only within this fixed DDL.
    // Existing logins are never silently rotated during an idempotent init.
    const string loginSql = """
        IF SUSER_ID(N'facturacion_runtime') IS NULL
        BEGIN
            DECLARE @statement nvarchar(max) =
                N'CREATE LOGIN [facturacion_runtime] WITH PASSWORD = ' +
                QUOTENAME(@password, '''') + N', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;';
            EXEC sys.sp_executesql @statement;
        END;
        """;
    await using (var login = new SqlCommand(loginSql, admin))
    {
        login.Parameters.Add("@password", System.Data.SqlDbType.NVarChar, 128).Value = password;
        await login.ExecuteNonQueryAsync();
    }

    await using var database = new SqlConnection(databaseOptions.ConnectionString);
    await database.OpenAsync();
    const string userSql = """
        IF USER_ID(N'facturacion_runtime') IS NULL
            CREATE USER [facturacion_runtime] FOR LOGIN [facturacion_runtime];
        ELSE
            ALTER USER [facturacion_runtime] WITH LOGIN = [facturacion_runtime];
        IF DATABASE_PRINCIPAL_ID(N'facturacion_runtime_role') IS NULL
            CREATE ROLE [facturacion_runtime_role] AUTHORIZATION [dbo];
        IF IS_ROLEMEMBER(N'facturacion_runtime_role', N'facturacion_runtime') <> 1
            ALTER ROLE [facturacion_runtime_role] ADD MEMBER [facturacion_runtime];
        REVOKE UPDATE, DELETE ON OBJECT::[dbo].[Facturas] FROM [facturacion_runtime_role];
        REVOKE UPDATE, DELETE ON OBJECT::[dbo].[DetalleFactura] FROM [facturacion_runtime_role];
        GRANT SELECT, INSERT ON OBJECT::[dbo].[Facturas] TO [facturacion_runtime_role];
        GRANT SELECT, INSERT ON OBJECT::[dbo].[DetalleFactura] TO [facturacion_runtime_role];
        """;
    await using (var user = new SqlCommand(userSql, database))
        await user.ExecuteNonQueryAsync();

    var runtimeOptions = new SqlConnectionStringBuilder(databaseOptions.ConnectionString)
    {
        IntegratedSecurity = false,
        UserID = RuntimeLogin,
        Password = password,
    };
    await using var runtime = new SqlConnection(runtimeOptions.ConnectionString);
    await runtime.OpenAsync();
    const string privilegeSql = """
        SELECT CASE WHEN IS_SRVROLEMEMBER(N'sysadmin') = 1
            OR IS_MEMBER(N'db_owner') = 1
            OR HAS_PERMS_BY_NAME(DB_NAME(), N'DATABASE', N'ALTER') = 1
            OR HAS_PERMS_BY_NAME(DB_NAME(), N'DATABASE', N'CREATE TABLE') = 1
            OR HAS_PERMS_BY_NAME(N'dbo', N'SCHEMA', N'ALTER') = 1
            OR HAS_PERMS_BY_NAME(N'dbo.Facturas', N'OBJECT', N'ALTER') = 1
            OR HAS_PERMS_BY_NAME(N'dbo.DetalleFactura', N'OBJECT', N'ALTER') = 1
            OR HAS_PERMS_BY_NAME(N'dbo.Facturas', N'OBJECT', N'UPDATE') = 1
            OR HAS_PERMS_BY_NAME(N'dbo.Facturas', N'OBJECT', N'DELETE') = 1
            OR HAS_PERMS_BY_NAME(N'dbo.DetalleFactura', N'OBJECT', N'UPDATE') = 1
            OR HAS_PERMS_BY_NAME(N'dbo.DetalleFactura', N'OBJECT', N'DELETE') = 1
            THEN 1 ELSE 0 END;
        """;
    await using var permissions = new SqlCommand(privilegeSql, runtime);
    if (await permissions.ExecuteScalarAsync() is not int privilegeResult || privilegeResult != 0)
        throw new InvalidOperationException("El usuario runtime tiene permisos administrativos, de esquema, actualización o borrado no permitidos.");
}
