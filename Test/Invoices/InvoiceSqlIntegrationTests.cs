using Domain.Entities;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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

    [Fact(SkipUnless = nameof(SqlAvailable))]
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

    [Fact(SkipUnless = nameof(SqlAvailable))]
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

    [Fact(SkipUnless = nameof(SqlAvailable))]
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

    [Fact(SkipUnless = nameof(SqlAvailable))]
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

    [Fact(SkipUnless = nameof(SqlAvailable))]
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

    private sealed class InvoiceTestDatabase(string databaseName, DbContextOptions<AppDbContext> options) : IAsyncDisposable
    {
        public AppDbContext Context() => new(options);

        public static async Task<InvoiceTestDatabase> CreateAsync()
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
                await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
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
