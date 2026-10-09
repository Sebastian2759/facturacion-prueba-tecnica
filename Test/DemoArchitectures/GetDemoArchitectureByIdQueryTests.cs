using System.Net;
using Application.Dtos;
using Application.UseCases.DemoArchitectures.GetDemoArchitectureById;
using Domain.Base.Interface;
using Domain.Contracts.Adapter.Mapper;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Test.DemoArchitectures;

public sealed class GetDemoArchitectureByIdQueryTests
{
    [Fact]
    public async Task Handle_WhenEntityDoesNotExist_ReturnsNotFound()
    {
        var request = new GetDemoArchitectureByIdRequest
        {
            Id = Guid.NewGuid(),
        };
        var cancellationToken = new CancellationTokenSource().Token;
        var repository = new Mock<IRepositoryGeneric<DemoArchitectureEntity>>();
        var mapper = new Mock<IMapperAdapter>(MockBehavior.Strict);

        repository
            .Setup(instance => instance.FindAsync(request.Id, cancellationToken))
            .ReturnsAsync((DemoArchitectureEntity?)null);

        var query = new GetDemoArchitectureByIdQuery(repository.Object, mapper.Object);

        var result = await query.Handle(request, cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        result.Data.Should().BeNull();
        repository.Verify(
            instance => instance.FindAsync(request.Id, cancellationToken),
            Times.Once);
        mapper.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenEntityExists_ReturnsMappedResponse()
    {
        var request = new GetDemoArchitectureByIdRequest
        {
            Id = Guid.NewGuid(),
        };
        var cancellationToken = new CancellationTokenSource().Token;
        var entity = new DemoArchitectureEntity
        {
            Id = request.Id,
            Name = "Clean Architecture",
            Description = "Flujo de referencia",
            IsActive = true,
        };
        var dto = new DemoArchitectureDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
        };
        var repository = new Mock<IRepositoryGeneric<DemoArchitectureEntity>>();
        var mapper = new Mock<IMapperAdapter>();

        repository
            .Setup(instance => instance.FindAsync(request.Id, cancellationToken))
            .ReturnsAsync(entity);
        mapper
            .Setup(instance =>
                instance.Map<DemoArchitectureEntity, DemoArchitectureDto>(entity))
            .Returns(dto);

        var query = new GetDemoArchitectureByIdQuery(repository.Object, mapper.Object);

        var result = await query.Handle(request, cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Should().NotBeNull();
        result.Data!.DemoArchitecture.Should().BeSameAs(dto);
        repository.Verify(
            instance => instance.FindAsync(request.Id, cancellationToken),
            Times.Once);
    }
}
