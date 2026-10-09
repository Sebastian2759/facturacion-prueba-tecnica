using Application;
using Application.Dtos;
using Application.UseCases.DemoArchitectures.GetDemoArchitectureById;
using Domain.Base.Interface;
using Domain.Contracts.Adapter.Mapper;
using Domain.Entities;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Test.Application;

public sealed class ApplicationServiceRegistrationTests
{
    [Fact]
    public async Task AddApplicationServices_ValidRequest_DispatchesToRegisteredQuery()
    {
        var request = new GetDemoArchitectureByIdRequest
        {
            Id = Guid.NewGuid(),
        };
        var entity = new DemoArchitectureEntity
        {
            Id = request.Id,
            Name = "Demo",
        };
        var dto = new DemoArchitectureDto
        {
            Id = entity.Id,
            Name = entity.Name,
        };
        var repository = new Mock<IRepositoryGeneric<DemoArchitectureEntity>>();
        var mapper = new Mock<IMapperAdapter>();
        repository
            .Setup(instance =>
                instance.FindAsync(
                    request.Id,
                    TestContext.Current.CancellationToken))
            .ReturnsAsync(entity);
        mapper
            .Setup(instance =>
                instance.Map<DemoArchitectureEntity, DemoArchitectureDto>(entity))
            .Returns(dto);
        using var provider = BuildProvider(repository.Object, mapper.Object);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(
            request,
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Data!.DemoArchitecture.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task AddApplicationServices_InvalidRequest_StopsBeforeQuery()
    {
        var repository = new Mock<IRepositoryGeneric<DemoArchitectureEntity>>(
            MockBehavior.Strict);
        var mapper = new Mock<IMapperAdapter>(MockBehavior.Strict);
        using var provider = BuildProvider(repository.Object, mapper.Object);
        var sender = provider.GetRequiredService<ISender>();

        Func<Task> action = async () =>
            await sender.Send(
                new GetDemoArchitectureByIdRequest(),
                TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<ValidationException>();
        repository.VerifyNoOtherCalls();
        mapper.VerifyNoOtherCalls();
    }

    private static ServiceProvider BuildProvider(
        IRepositoryGeneric<DemoArchitectureEntity> repository,
        IMapperAdapter mapper)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(repository);
        services.AddSingleton(mapper);
        services.AddApplicationServices();

        return services.BuildServiceProvider();
    }
}
