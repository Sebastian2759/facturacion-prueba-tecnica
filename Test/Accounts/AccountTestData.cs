using Application.UseCases.Accounts.CreateAccount;
using Domain.Contracts.Accounts;
using Domain.Contracts.Security;

namespace Test.Accounts;

internal static class AccountTestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 9, 14, 0, 0, TimeSpan.Zero);

    public static AccountRecord Record(string code = "TEST001") => new(
        code, "CC", "900000010", "Persona Ficticia", "Persona", "Ficticia", "test@example.invalid", 1,
        "11001", "11", "169", "Calle ficticia 10", null, "3000000010", 1, "https://example.invalid", Now, "EXT-1");

    public static CreateAccountRequest Request(string mode = "simulated", string code = "TEST001") => new()
    {
        Mode = mode,
        Code = code,
        IdentificationType = "CC",
        Document = "900000010",
        Name = "Persona Ficticia",
        FirstName = "Persona",
        LastName = "Ficticia",
        Email = "test@example.invalid",
        PersonType = 1,
        CityCode = "11001",
        DepartmentCode = "11",
        CountryCode = "169",
        Address = "Calle ficticia 10",
        Phone = "3000000010",
        CustomerType = 1,
        Website = "https://example.invalid",
        RegisteredAt = Now,
        ExternalCode = "EXT-1",
    };
}

internal sealed class AccountsCurrentUser(string subject = "issuer|alice", bool canEvaluate = true, bool canUseRealAccounts = false) : ICurrentUser
{
    public string Subject { get; } = subject;
    public bool CanEvaluate { get; } = canEvaluate;
    public bool CanUseRealAccounts { get; } = canUseRealAccounts;
}

internal sealed class AccountsClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = AccountTestData.Now;
    public override DateTimeOffset GetUtcNow() => Now;
}
