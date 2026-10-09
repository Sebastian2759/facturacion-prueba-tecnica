using Domain.Base.Interface;
using Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Persistence.Base;
using Persistence.Configuration;
using Persistence.Context;

namespace Test.DemoArchitectures;

public sealed class DemoArchitecturePersistenceTests
{
    private const string ConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=CleanArchitectureTemplateTests;Trusted_Connection=True;";

    [Fact]
    public void Model_UsesDemoArchitectureConfiguration()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        using var context = new AppDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(DemoArchitectureEntity));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be(DemoArchitectureConfiguration.TableName);
        entityType.FindProperty(nameof(DemoArchitectureEntity.Name))!
            .GetMaxLength()
            .Should()
            .Be(DemoArchitectureConfiguration.NameMaxLength);
        entityType.FindProperty(nameof(DemoArchitectureEntity.Description))!
            .GetMaxLength()
            .Should()
            .Be(DemoArchitectureConfiguration.DescriptionMaxLength);
    }

    [Fact]
    public void AddPersistenceServices_ResolvesGenericRepositoryAsScoped()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AppDbContext"] = ConnectionString,
            })
            .Build();
        var services = new ServiceCollection();

        services.AddPersistenceServices(configuration);

        var descriptor = services.Single(service =>
            service.ServiceType == typeof(IRepositoryGeneric<>));
        descriptor.ImplementationType.Should().Be(typeof(RepositoryGeneric<>));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider
            .GetRequiredService<IRepositoryGeneric<DemoArchitectureEntity>>();

        repository.Should().BeOfType<RepositoryGeneric<DemoArchitectureEntity>>();
    }
}
