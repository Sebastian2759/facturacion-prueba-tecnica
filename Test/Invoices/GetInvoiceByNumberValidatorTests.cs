using Application.UseCases.Invoices.GetInvoiceByNumber;
using FluentAssertions;

namespace Test.Invoices;

public sealed class GetInvoiceByNumberValidatorTests
{
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task Validate_Number_MustBePositive(int number, bool expected)
    {
        var result = await new GetInvoiceByNumberValidator().ValidateAsync(
            new GetInvoiceByNumberRequest { Number = number }, TestContext.Current.CancellationToken);
        result.IsValid.Should().Be(expected);
    }
}
