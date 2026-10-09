using Api.Controllers.v1;
using Application.Base;
using Application.UseCases.Accounts.CreateAccount;
using Application.UseCases.Accounts.GetAccounts;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Test.Accounts;

public sealed class AccountsControllerTests
{
    [Fact]
    public async Task Get_SendsQueryAndPreservesResultStatusAndTrace()
    {
        var request = new GetAccountsRequest { Code = "TEST001" };
        var expected = ResponseBase<GetAccountsResponse>.Ok(new("simulated", []));
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(instance => instance.Send(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = CreateController(mediator.Object);

        var result = Assert.IsType<ObjectResult>(await controller.Get(request, CancellationToken.None));

        Assert.Equal(200, result.StatusCode);
        var response = Assert.IsType<ResponseBase<GetAccountsResponse>>(result.Value);
        Assert.Equal(expected.Data, response.Data);
        Assert.Equal("test-trace", response.TraceId);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task Create_SendsCommandAndPreservesAcceptedCodeAnd201()
    {
        var request = AccountTestData.Request();
        var expected = ResponseBase<CreateAccountResponse>.Created(new("simulated", request.Code, true));
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(instance => instance.Send(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = CreateController(mediator.Object);

        var result = Assert.IsType<ObjectResult>(await controller.Create(request, CancellationToken.None));

        Assert.Equal(201, result.StatusCode);
        var response = Assert.IsType<ResponseBase<CreateAccountResponse>>(result.Value);
        Assert.True(response.Data!.Accepted);
        Assert.Equal(request.Code, response.Data.Code);
        Assert.Equal("test-trace", response.TraceId);
        mediator.VerifyAll();
    }

    private static AccountsController CreateController(IMediator mediator) => new(mediator)
    {
        ControllerContext = new() { HttpContext = new DefaultHttpContext { TraceIdentifier = "test-trace" } },
    };
}
