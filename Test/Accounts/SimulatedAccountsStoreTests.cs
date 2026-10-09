using Adapters.Accounts;
using Domain.Contracts.Accounts;

namespace Test.Accounts;

public sealed class SimulatedAccountsStoreTests
{
    [Fact]
    public void UserCreatedAccountsAreIsolatedBySubject()
    {
        var store = new SimulatedAccountsStore(new AccountsClock());
        store.Create("alice", AccountTestData.Record());

        Assert.Single(store.Get("alice", "TEST001"));
        Assert.Empty(store.Get("bob", "TEST001"));
        Assert.Equal(2, store.Get("bob", null).Count);
    }

    [Fact]
    public void DuplicateCodeCannotOverwriteExampleOrOwnedAccount()
    {
        var store = new SimulatedAccountsStore(new AccountsClock());
        store.Create("alice", AccountTestData.Record());

        Assert.Equal(409, Assert.Throws<AccountsIntegrationException>(() => store.Create("alice", AccountTestData.Record("test001"))).HttpStatus);
        Assert.Equal(409, Assert.Throws<AccountsIntegrationException>(() => store.Create("alice", AccountTestData.Record("demo001"))).HttpStatus);
        store.Create("bob", AccountTestData.Record());
    }

    [Fact]
    public void EntriesExpireAfterSevenDays()
    {
        var clock = new AccountsClock();
        var store = new SimulatedAccountsStore(clock);
        store.Create("alice", AccountTestData.Record());
        clock.Now = clock.Now.AddDays(7);

        Assert.Empty(store.Get("alice", "TEST001"));
    }

    [Fact]
    public void AccountCapacityIsBounded()
    {
        var store = new SimulatedAccountsStore(new AccountsClock());
        for (var index = 0; index < 100; index++)
            store.Create("alice", AccountTestData.Record($"T{index}"));

        Assert.Equal(429, Assert.Throws<AccountsIntegrationException>(() => store.Create("alice", AccountTestData.Record("OVER"))).HttpStatus);
    }
}
