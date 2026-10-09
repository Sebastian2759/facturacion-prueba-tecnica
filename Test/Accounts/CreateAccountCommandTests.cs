using Application.UseCases.Accounts.CreateAccount;
using Domain.Base;
using Domain.Contracts.Accounts;
using Moq;

namespace Test.Accounts;

public sealed class CreateAccountCommandTests
{
    [Fact]
    public async Task Handle_CreatesAccountWithAuthenticatedOwnerAndReportsAcceptance()
    {
        var gateway = new Mock<IAccountsGateway>(MockBehavior.Strict);
        gateway.Setup(instance => instance.CreateAsync(AccountExecutionMode.Simulated, "issuer|alice", AccountTestData.Record(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var command = new CreateAccountCommand(gateway.Object, new AccountsCurrentUser());

        var response = await command.Handle(AccountTestData.Request(), CancellationToken.None);

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("simulated", response.Data!.Mode);
        Assert.Equal("TEST001", response.Data.Code);
        Assert.True(response.Data.Accepted);
        gateway.VerifyAll();
    }

    [Fact]
    public async Task Handle_RealWriteRequiresSeparatePermission()
    {
        var gateway = new Mock<IAccountsGateway>(MockBehavior.Strict);
        var command = new CreateAccountCommand(gateway.Object, new AccountsCurrentUser());

        await Assert.ThrowsAsync<AccessDeniedException>(() => command.Handle(AccountTestData.Request("real"), CancellationToken.None));

        gateway.VerifyNoOtherCalls();
    }
}
