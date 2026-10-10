using Domain.Entities;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Persistence.Context;
using Persistence.Repositories;

namespace Test.Invoices;

/// <summary>
/// Requires an explicitly configured, disposable SQL Server test server. Each
/// test creates a uniquely named database; no application database is reused.
/// </summary>
public sealed class InvoiceSqlIntegrationTests
{
    public static bool SqlAvailable =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FACTURACION_TEST_SQL_CONNECTION"));

    [Fact(Skip = "Requires a dedicated SQL Server via FACTURACION_TEST_SQL_CONNECTION.", SkipUnless = nameof(SqlAvailable))]
    [Trait("Category", "SqlIntegration")]
    public async Task VarcharMigration_PreservesExistingUnicodeAndVerifiesAllRequiredSqlTypes()
    {
        await using var database = await InvoiceTestDatabase.CreateAsync("20261009000100_InitialInvoices");
        await using var context = database.Context();
        var entity = InvoiceTestData.Entity();
        // Raw Unicode literals seed the previous schema independently of the new EF mapping.
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO dbo.Facturas (Id,Date,OwnerSubject,IdempotencyKey,RequestFingerprint,
                Document,FirstName,LastName,Address,Phone,Gross,Discount,Tax,Total,CreatedAtUtc,UpdatedAtUtc)
            SELECT {entity.Id},{entity.Date},{entity.OwnerSubject},{entity.IdempotencyKey},
                {entity.RequestFingerprint},{entity.Document},N'José 漢字',N'Muñoz',N'Calle Ñ 日本',N'+57 123',
                {entity.Gross},{entity.Discount},{entity.Tax},{entity.Total},{entity.CreatedAtUtc},{entity.UpdatedAtUtc}
            """, TestContext.Current.CancellationToken);
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
        var invoice = await context.Invoices.SingleAsync(TestContext.Current.CancellationToken);
        invoice.FirstName.Should().Be("José 漢字");
        invoice.LastName.Should().Be("Muñoz");
        invoice.Address.Should().Be("Calle Ñ 日本");
        invoice.Phone.Should().Be("+57 123");
        var unicodeInvoice = InvoiceTestData.Entity();
        unicodeInvoice.FirstName = new string('漢', 100);
        unicodeInvoice.LastName = "Muñoz 日本";
        unicodeInvoice.Address = "Calle Ñ 東京";
        context.Invoices.Add(unicodeInvoice);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();
        var savedUnicode = await context.Invoices.SingleAsync(item => item.Id == unicodeInvoice.Id,
            TestContext.Current.CancellationToken);
        savedUnicode.FirstName.Should().Be(unicodeInvoice.FirstName);
        savedUnicode.LastName.Should().Be(unicodeInvoice.LastName);
        savedUnicode.Address.Should().Be(unicodeInvoice.Address);
        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT OBJECT_NAME(c.object_id) + '.' + c.name, t.name, c.max_length
            FROM sys.columns c JOIN sys.types t ON c.user_type_id=t.user_type_id
            WHERE c.object_id IN (OBJECT_ID('dbo.Facturas'), OBJECT_ID('dbo.DetalleFactura'));
            """;
        var columns = new Dictionary<string, (string Type, short Length)>();
        await using (var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken))
            while (await reader.ReadAsync(TestContext.Current.CancellationToken))
                columns.Add(reader.GetString(0), (reader.GetString(1), reader.GetInt16(2)));
        columns["Facturas.Document"].Should().Be(("varchar", (short)15));
        columns["Facturas.FirstName"].Should().Be(("varchar", (short)400));
        columns["Facturas.LastName"].Should().Be(("varchar", (short)400));
        columns["Facturas.Address"].Should().Be(("varchar", (short)800));
        columns["Facturas.Phone"].Should().Be(("varchar", (short)120));
        columns["Facturas.Date"].Type.Should().Be("datetime");
        columns["Facturas.Number"].Type.Should().Be("int");
        columns["DetalleFactura.Code"].Type.Should().Be("int");
        columns["DetalleFactura.Quantity"].Type.Should().Be("int");
        foreach (var name in new[] { "Gross", "Discount", "Tax", "Total" })
            columns["Facturas." + name].Type.Should().Be("money");
        columns["DetalleFactura.UnitPrice"].Type.Should().Be("money");
    }

    [Fact(Skip = "Requires a dedicated SQL Server via FACTURACION_TEST_SQL_CONNECTION.", SkipUnless = nameof(SqlAvailable))]
    [Trait("Category", "SqlIntegration")]
    public async Task Create_RoundTripsAggregateAndFiltersDifferentOwners()
    {
        await using var database = await InvoiceTestDatabase.CreateAsync();
        var key = Guid.NewGuid();
        int firstNumber;
        await using (var context = database.Context())
        {
            var repository = new InvoiceRepository(context);
            var first = await repository.CreateOrGetAsync(InvoiceTestData.Entity("owner-a", key), TestContext.Current.CancellationToken);
            first.WasCreated.Should().BeTrue();
            firstNumber = first.Invoice.Number;
            firstNumber.Should().BeGreaterThan(0);
        }
        await using (var context = database.Context())
        {
            var repository = new InvoiceRepository(context);
            var second = await repository.CreateOrGetAsync(InvoiceTestData.Entity("owner-b", key), TestContext.Current.CancellationToken);
            second.Invoice.Number.Should().BeGreaterThan(firstNumber);
        }
        await using (var context = database.Context())
        {
            var repository = new InvoiceRepository(context);
            var invoice = await repository.FindOwnedByNumberAsync("owner-a", firstNumber, TestContext.Current.CancellationToken);
            invoice.Should().NotBeNull();
            invoice!.Total.Should().Be(565_250m);
            invoice.Details.Should().ContainSingle();
            invoice.Details.Single().UnitPrice.Should().Be(250_000m);
            (await repository.FindOwnedByNumberAsync("owner-b", firstNumber, TestContext.Current.CancellationToken))
                .Should().BeNull();
            (await repository.ListOwnedAsync("owner-a", 1, 10, TestContext.Current.CancellationToken))
                .TotalCount.Should().Be(1);
            (await repository.FindOwnedByIdempotencyKeyAsync("OWNER-A", key, TestContext.Current.CancellationToken))
                .Should().BeNull("owner identifiers must not be compared case-insensitively");
        }
    }

    [Fact(Skip = "Requires a dedicated SQL Server via FACTURACION_TEST_SQL_CONNECTION.", SkipUnless = nameof(SqlAvailable))]
    [Trait("Category", "SqlIntegration")]
    public async Task ConcurrentSameKey_InsertsExactlyOneAggregate()
    {
        await using var database = await InvoiceTestDatabase.CreateAsync();
        var key = Guid.NewGuid();
        await using var firstContext = database.Context();
        await using var secondContext = database.Context();
        var first = new InvoiceRepository(firstContext).CreateOrGetAsync(
            InvoiceTestData.Entity("owner-a", key), TestContext.Current.CancellationToken);
        var second = new InvoiceRepository(secondContext).CreateOrGetAsync(
            InvoiceTestData.Entity("owner-a", key), TestContext.Current.CancellationToken);

        var results = await Task.WhenAll(first, second);

        results.Select(result => result.Invoice.Number).Distinct().Should().ContainSingle();
        results.Count(result => result.WasCreated).Should().Be(1);
        await using var readContext = database.Context();
        (await readContext.Invoices.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        (await readContext.InvoiceLines.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact(Skip = "Requires a dedicated SQL Server via FACTURACION_TEST_SQL_CONNECTION.", SkipUnless = nameof(SqlAvailable))]
    [Trait("Category", "SqlIntegration")]
    public async Task InvalidDetail_RollsBackHeaderAndAllLines()
    {
        await using var database = await InvoiceTestDatabase.CreateAsync();
        await using (var context = database.Context())
        {
            var invoice = InvoiceTestData.Entity();
            invoice.Details.Single().Quantity = 0;
            var repository = new InvoiceRepository(context);
            Func<Task> action = () => repository.CreateOrGetAsync(invoice, TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<DbUpdateException>();
        }
        await using var verifyContext = database.Context();
        (await verifyContext.Invoices.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        (await verifyContext.InvoiceLines.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact(Skip = "Requires a dedicated SQL Server via FACTURACION_TEST_SQL_CONNECTION.", SkipUnless = nameof(SqlAvailable))]
    [Trait("Category", "SqlIntegration")]
    public async Task List_PaginatesByDescendingNumberWithoutLeakingOtherOwner()
    {
        await using var database = await InvoiceTestDatabase.CreateAsync();
        await using (var context = database.Context())
        {
            var repository = new InvoiceRepository(context);
            await repository.CreateOrGetAsync(InvoiceTestData.Entity(), TestContext.Current.CancellationToken);
            await repository.CreateOrGetAsync(InvoiceTestData.Entity(), TestContext.Current.CancellationToken);
            await repository.CreateOrGetAsync(InvoiceTestData.Entity("owner-b"), TestContext.Current.CancellationToken);
        }
        await using var readContext = database.Context();
        var readRepository = new InvoiceRepository(readContext);
        var first = await readRepository.ListOwnedAsync("owner-a", 1, 1, TestContext.Current.CancellationToken);
        var second = await readRepository.ListOwnedAsync("owner-a", 2, 1, TestContext.Current.CancellationToken);

        first.TotalCount.Should().Be(2);
        second.TotalCount.Should().Be(2);
        first.Items.Should().ContainSingle();
        second.Items.Should().ContainSingle();
        first.Items[0].Number.Should().BeGreaterThan(second.Items[0].Number);
        first.Items.Concat(second.Items).Should().OnlyContain(invoice => invoice.OwnerSubject == "owner-a");
    }

    [Fact(Skip = "Requires a dedicated SQL Server via FACTURACION_TEST_SQL_CONNECTION.", SkipUnless = nameof(SqlAvailable))]
    [Trait("Category", "SqlIntegration")]
    public async Task ReadInsertOnly_CreatesReadsAndReplaysWithoutUpdateOrDelete()
    {
        await using var database = await InvoiceTestDatabase.CreateAsync();
        await using var adminContext = database.Context();
        await adminContext.Database.ExecuteSqlRawAsync("""
            CREATE USER [invoice_read_insert_test] WITHOUT LOGIN;
            GRANT SELECT, INSERT ON OBJECT::[dbo].[Facturas] TO [invoice_read_insert_test];
            GRANT SELECT, INSERT ON OBJECT::[dbo].[DetalleFactura] TO [invoice_read_insert_test];
            """, TestContext.Current.CancellationToken);

        // Impersonation is confined to this non-pooled connection and disposable
        // database; the test never creates or changes the application's login.
        var connectionOptions = new SqlConnectionStringBuilder(adminContext.Database.GetConnectionString())
        {
            Pooling = false,
        };
        await using var connection = new SqlConnection(connectionOptions.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using (var impersonate = new SqlCommand("EXECUTE AS USER = N'invoice_read_insert_test';", connection))
            await impersonate.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options;
        await using var runtimeContext = new AppDbContext(options);
        var repository = new InvoiceRepository(runtimeContext);
        var key = Guid.NewGuid();
        var created = await repository.CreateOrGetAsync(
            InvoiceTestData.Entity("owner-a", key), TestContext.Current.CancellationToken);
        var replay = await repository.CreateOrGetAsync(
            InvoiceTestData.Entity("owner-a", key), TestContext.Current.CancellationToken);

        created.WasCreated.Should().BeTrue();
        replay.WasCreated.Should().BeFalse();
        replay.Invoice.Number.Should().Be(created.Invoice.Number);
        var invoice = await repository.FindOwnedByNumberAsync(
            "owner-a", created.Invoice.Number, TestContext.Current.CancellationToken);
        invoice.Should().NotBeNull();
        invoice!.Details.Should().ContainSingle();
        (await repository.ListOwnedAsync("owner-a", 1, 10, TestContext.Current.CancellationToken))
            .TotalCount.Should().Be(1);

        string[] forbiddenStatements =
        [
            "UPDATE [dbo].[Facturas] SET [Total] = [Total] WHERE 1 = 0;",
            "DELETE FROM [dbo].[Facturas] WHERE 1 = 0;",
            "UPDATE [dbo].[DetalleFactura] SET [Quantity] = [Quantity] WHERE 1 = 0;",
            "DELETE FROM [dbo].[DetalleFactura] WHERE 1 = 0;",
        ];
        foreach (var statement in forbiddenStatements)
        {
            Func<Task> action = async () =>
            {
                await using var command = new SqlCommand(statement, connection);
                await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            };
            await action.Should().ThrowAsync<SqlException>().Where(exception => exception.Number == 229);
        }
    }

    [Fact(Skip = "Requires dedicated SQL Server.", SkipUnless = nameof(SqlAvailable))]
    [Trait("Category", "SqlIntegration")]
    public async Task RetireTemplateExample_RemovesEmptyTableAndCanRollback()
    {
        await using var database = await InvoiceTestDatabase.CreateAsync();
        await using var context = database.Context();
        var tables = await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sys.tables WHERE name='DemoArchitectures'").SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, tables);
        await context.GetService<IMigrator>().MigrateAsync("20261010000100_InvoiceVarcharFields", TestContext.Current.CancellationToken);
        tables = await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sys.tables WHERE name='DemoArchitectures'").SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, tables);
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Skip = "Requires dedicated SQL Server.", SkipUnless = nameof(SqlAvailable))]
    [Trait("Category", "SqlIntegration")]
    public async Task RetireTemplateExample_RefusesToEraseUnexpectedData()
    {
        await using var database = await InvoiceTestDatabase.CreateAsync("20261010000100_InvoiceVarcharFields");
        await using var context = database.Context();
        await context.Database.ExecuteSqlRawAsync("INSERT dbo.DemoArchitectures (Id, Name, Description, CreatedAtUtc, UpdatedAtUtc) VALUES (NEWID(), N'Legacy', N'Preserve', SYSUTCDATETIME(), SYSUTCDATETIME())", TestContext.Current.CancellationToken);
        var failure = await Assert.ThrowsAsync<SqlException>(() => context.Database.MigrateAsync(TestContext.Current.CancellationToken));
        Assert.Equal(51001, failure.Number);
        var count = await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM dbo.DemoArchitectures").SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, count);
    }

    private sealed class InvoiceTestDatabase(string databaseName, DbContextOptions<AppDbContext> options) : IAsyncDisposable
    {
        public AppDbContext Context() => new(options);

        public static async Task<InvoiceTestDatabase> CreateAsync(string? targetMigration = null)
        {
            var configured = Environment.GetEnvironmentVariable("FACTURACION_TEST_SQL_CONNECTION");
            Assert.SkipWhen(string.IsNullOrWhiteSpace(configured),
                "SQL Server integration was not run: set FACTURACION_TEST_SQL_CONNECTION to a dedicated test server.");
            var name = "InvoiceTests_" + Guid.NewGuid().ToString("N");
            var connection = new SqlConnectionStringBuilder(configured)
            {
                InitialCatalog = name,
            };
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connection.ConnectionString).Options;
            var database = new InvoiceTestDatabase(name, options);
            try
            {
                await using var context = database.Context();
                await context.GetService<IMigrator>().MigrateAsync(targetMigration, TestContext.Current.CancellationToken);
                return database;
            }
            catch
            {
                await database.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await using var context = Context();
            var actualName = new SqlConnectionStringBuilder(context.Database.GetConnectionString()).InitialCatalog;
            if (actualName != databaseName || !databaseName.StartsWith("InvoiceTests_", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing to delete a database outside this test fixture.");
            await context.Database.EnsureDeletedAsync();
        }
    }
}
