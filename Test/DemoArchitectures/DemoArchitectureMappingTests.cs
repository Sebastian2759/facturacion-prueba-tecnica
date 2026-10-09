using Adapters;
using Application.Dtos;
using Domain.Contracts.Adapter.Mapper;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Test.DemoArchitectures;

public sealed class DemoArchitectureMappingTests
{
    [Fact]
    public void AutoConventionProfile_MapsEntityToDtoWithoutSpecificProfile()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAdapterServices();
        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapperAdapter>();
        var entity = new DemoArchitectureEntity
        {
            Id = Guid.NewGuid(),
            Name = "Demo",
            Description = "Mapeo por convención",
            IsActive = true,
        };

        var dto = mapper.Map<DemoArchitectureEntity, DemoArchitectureDto>(entity);

        dto.Should().BeEquivalentTo(new
        {
            entity.Id,
            entity.Name,
            entity.Description,
            entity.IsActive,
        });
    }
}
