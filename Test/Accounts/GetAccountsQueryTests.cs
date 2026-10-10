using Application.UseCases.Accounts.GetAccounts;
using Domain.Base;
using Domain.Contracts.Accounts;
using Moq;

namespace Test.Accounts;

public sealed class GetAccountsQueryTests
{
    [Fact]
    public async Task Handle_UsesValidatedSubjectAndMapsAllProviderNeutralFields()
    {
        var account = AccountTestData.Record();
        var gateway = new Mock<IAccountsGateway>(MockBehavior.Strict);
        gateway.Setup(instance => instance.GetAsync("TEST001", It.IsAny<CancellationToken>()))
            .ReturnsAsync([account]);
        var query = new GetAccountsQuery(gateway.Object, new AccountsCurrentUser(canUseRealAccounts: true));

        var response = await query.Handle(new() { Code = "TEST001" }, CancellationToken.None);

        Assert.True(response.IsSuccess);
        var actual = Assert.Single(response.Data!.Accounts);
        Assert.Equal(account.Code, actual.Code);
        Assert.Equal(account.IdentificationType, actual.IdentificationType);
        Assert.Equal(account.Document, actual.Document);
        Assert.Equal(account.Name, actual.Name);
        Assert.Equal(account.FirstName, actual.FirstName);
        Assert.Equal(account.LastName, actual.LastName);
        Assert.Equal(account.Email, actual.Email);
        Assert.Equal(account.PersonType, actual.PersonType);
        Assert.Equal(account.CityCode, actual.CityCode);
        Assert.Equal(account.DepartmentCode, actual.DepartmentCode);
        Assert.Equal(account.CountryCode, actual.CountryCode);
        Assert.Equal(account.Address, actual.Address);
        Assert.Equal(account.AdditionalAddress, actual.AdditionalAddress);
        Assert.Equal(account.Phone, actual.Phone);
        Assert.Equal(account.CustomerType, actual.CustomerType);
        Assert.Equal(account.Website, actual.Website);
        Assert.Equal(account.RegisteredAt, actual.RegisteredAt);
        Assert.Equal(account.ExternalCode, actual.ExternalCode);
        gateway.VerifyAll();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Handle_RejectsMissingPermissionBeforeCallingProvider(bool evaluate, bool real)
    {
        var gateway = new Mock<IAccountsGateway>(MockBehavior.Strict);
        var query = new GetAccountsQuery(gateway.Object, new AccountsCurrentUser(canEvaluate: evaluate, canUseRealAccounts: real));

        await Assert.ThrowsAsync<AccessDeniedException>(() => query.Handle(new(), CancellationToken.None));

        gateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_PropagatesProviderFailure()
    {
        var gateway = new Mock<IAccountsGateway>(MockBehavior.Strict);
        gateway.Setup(instance => instance.GetAsync(null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccountsIntegrationException("PROVIDER_TIMEOUT", 504, "Tiempo agotado."));
        var query = new GetAccountsQuery(gateway.Object, new AccountsCurrentUser(canUseRealAccounts: true));

        var failure = await Assert.ThrowsAsync<AccountsIntegrationException>(() => query.Handle(new(), CancellationToken.None));

        Assert.Equal("PROVIDER_TIMEOUT", failure.Code);
        gateway.Verify(instance => instance.GetAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
