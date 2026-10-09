using Domain.Contracts.Persistence;
using Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Persistence.Context;
using Persistence.Repositories;

namespace Test.Invoices;

public sealed class InvoicePersistenceModelTests
{
    private static readonly string[] IdempotencyIndexProperties = ["OwnerSubject", "IdempotencyKey"];
    private const string ModelOnlyConnection =
        "Server=localhost;Database=InvoiceModelOnly;Integrated Security=true;Encrypt=true;";

    [Fact]
    public void Model_MapsIdentityMoneyDateAndOwnerScopedUniqueness()
    {
        using var context = Context();
        var invoice = context.Model.FindEntityType(typeof(InvoiceEntity))!;
        var line = context.Model.FindEntityType(typeof(InvoiceLineEntity))!;

        invoice.GetTableName().Should().Be("Facturas");
        line.GetTableName().Should().Be("DetalleFactura");
        invoice.FindProperty(nameof(InvoiceEntity.Number))!.GetValueGenerationStrategy()
            .Should().Be(SqlServerValueGenerationStrategy.IdentityColumn);
        invoice.FindProperty(nameof(InvoiceEntity.Id))!.ValueGenerated.Should().Be(ValueGenerated.Never);
        invoice.FindProperty(nameof(InvoiceEntity.Document))!.GetColumnType().Should().Be("varchar(15)");
        invoice.FindProperty(nameof(InvoiceEntity.Date))!.GetColumnType().Should().Be("datetime");
        line.FindProperty(nameof(InvoiceLineEntity.UnitPrice))!.GetColumnType().Should().Be("money");
        line.FindProperty(nameof(InvoiceLineEntity.Code))!.GetColumnType().Should().Be("int");
        invoice.GetIndexes().Should().Contain(index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(IdempotencyIndexProperties));
        line.GetForeignKeys().Should().ContainSingle().Which.PrincipalEntityType.Should().BeSameAs(invoice);
    }

    [Fact]
    public void MigrationSnapshot_MatchesCurrentModel_AndScriptIncludesRequiredSchema()
    {
        using var context = Context();

        context.Database.HasPendingModelChanges().Should().BeFalse(
            "a release must not run a hand-written migration with an unverified model mismatch");
        var script = context.GetService<IMigrator>().GenerateScript();
        script.Should().Contain("CREATE TABLE [dbo].[Facturas]");
        script.Should().Contain("IDENTITY");
        script.Should().Contain("CREATE TABLE [dbo].[DetalleFactura]");
        script.Should().Contain("IX_Facturas_OwnerSubject_IdempotencyKey");
    }

    [Fact]
    public void DependencyConvention_ResolvesSpecializedInvoiceRepositoryAsScoped()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:AppDbContext"] = ModelOnlyConnection,
        }).Build();
        var services = new ServiceCollection();
        services.AddPersistenceServices(configuration);

        services.Single(service => service.ServiceType == typeof(IInvoiceRepository))
            .Lifetime.Should().Be(ServiceLifetime.Scoped);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IInvoiceRepository>().Should().BeOfType<InvoiceRepository>();
    }

    private static AppDbContext Context()
        => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ModelOnlyConnection).Options);
}
