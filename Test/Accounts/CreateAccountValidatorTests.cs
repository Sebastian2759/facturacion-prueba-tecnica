using Application.UseCases.Accounts.CreateAccount;
using Application.UseCases.Accounts.GetAccounts;

namespace Test.Accounts;

public sealed class CreateAccountValidatorTests
{
    [Fact]
    public void Validate_AcceptsDocumentedPayload() => Assert.True(new CreateAccountValidator().Validate(AccountTestData.Request()).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("../Accounts")]
    [InlineData("A?code=other")]
    [InlineData("ABCDEFGHIJKLMNOP")]
    [InlineData("A B")]
    public void Validate_RejectsInvalidCode(string code) => Assert.Contains(
        new CreateAccountValidator().Validate(AccountTestData.Request(code: code)).Errors, failure => failure.PropertyName == "Code");

    [Fact]
    public void Validate_RejectsMissingRequiredFieldsAndInvalidRanges()
    {
        var result = new CreateAccountValidator().Validate(new CreateAccountRequest { Mode = "untrusted", PersonType = 3, CustomerType = 6 });

        foreach (var property in new[] { "Mode", "Code", "IdentificationType", "Document", "Name", "Email", "PersonType", "CityCode", "DepartmentCode", "CountryCode", "CustomerType", "RegisteredAt" })
            Assert.Contains(result.Errors, failure => failure.PropertyName == property);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user:password@example.invalid")]
    [InlineData("file:///secret")]
    public void Validate_RejectsUnsafeWebsite(string website)
    {
        var result = new CreateAccountValidator().Validate(new CreateAccountRequest { Website = website });
        Assert.Contains(result.Errors, failure => failure.PropertyName == "Website");
    }
}

public sealed class GetAccountsValidatorTests
{
    [Fact]
    public void Validate_AllowsNoCodeForListAll() => Assert.True(new GetAccountsValidator().Validate(new GetAccountsRequest()).IsValid);

    [Theory]
    [InlineData("../x")]
    [InlineData("")]
    [InlineData("1234567890123456")]
    public void Validate_RejectsInvalidPathCode(string code) => Assert.False(new GetAccountsValidator().Validate(new GetAccountsRequest { Code = code }).IsValid);
}
