using Api.Controllers.v1;
using Application.Base;
using Application.Dtos;
using Application.UseCases.DemoArchitectures.GetDemoArchitectureById;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Test.DemoArchitectures;

public sealed class DemoArchitecturesControllerTests
{
    [Fact]
    public async Task GetById_SendsRequestAndReturnsMediatorResult()
    {
        var request = new GetDemoArchitectureByIdRequest
        {
            Id = Guid.NewGuid(),
        };
        var cancellationToken = new CancellationTokenSource().Token;
        var response = ResponseBase<GetDemoArchitectureByIdResponse>.Ok(
            new GetDemoArchitectureByIdResponse(
                new DemoArchitectureDto
                {
                    Id = request.Id,
                    Name = "Demo",
                }));
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(instance => instance.Send(request, cancellationToken))
            .ReturnsAsync(response);
        var controller = new DemoArchitecturesController(mediator.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { TraceIdentifier = "trace-test" } };

        var actionResult = await controller.GetById(request, cancellationToken);

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(200);
        var result = objectResult.Value.Should().BeOfType<ResponseBase<GetDemoArchitectureByIdResponse>>().Subject;
        result.Data.Should().BeEquivalentTo(response.Data);
        result.TraceId.Should().Be("trace-test");
        mediator.Verify(
            instance => instance.Send(request, cancellationToken),
            Times.Once);
    }
}
