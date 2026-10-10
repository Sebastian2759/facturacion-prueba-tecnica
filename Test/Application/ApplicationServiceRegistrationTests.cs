using Application;
using Application.UseCases.Accounts.GetAccounts;
using Domain.Contracts.Accounts;
using Domain.Contracts.Security;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Test.Accounts;

namespace Test.Application;

public sealed class ApplicationServiceRegistrationTests
{
    [Fact]
    public async Task AddApplicationServices_DispatchesValidatedAccountQuery()
    {
        var gateway = new Mock<IAccountsGateway>(MockBehavior.Strict);
        gateway.Setup(x => x.GetAsync("TEST001", It.IsAny<CancellationToken>())).ReturnsAsync([AccountTestData.Record()]);
        using var provider = BuildProvider(gateway.Object);
        var result = await provider.GetRequiredService<ISender>().Send(new GetAccountsRequest { Code = "TEST001" }, TestContext.Current.CancellationToken);
        Assert.Equal("TEST001", Assert.Single(result.Data!.Accounts).Code);
        gateway.VerifyAll();
    }

    [Fact]
    public async Task AddApplicationServices_RejectsInvalidRequestBeforeProvider()
    {
        var gateway = new Mock<IAccountsGateway>(MockBehavior.Strict);
        using var provider = BuildProvider(gateway.Object);
        await Assert.ThrowsAsync<ValidationException>(() => provider.GetRequiredService<ISender>().Send(new GetAccountsRequest { Code = "../invalid" }, TestContext.Current.CancellationToken));
        gateway.VerifyNoOtherCalls();
    }

    private static ServiceProvider BuildProvider(IAccountsGateway gateway)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(gateway);
        services.AddSingleton<ICurrentUser>(new AccountsCurrentUser(canUseRealAccounts: true));
        services.AddApplicationServices();
        return services.BuildServiceProvider();
    }
}
