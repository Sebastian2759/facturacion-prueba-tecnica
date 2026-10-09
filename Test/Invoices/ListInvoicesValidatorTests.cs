using Application.UseCases.Invoices.ListInvoices;
using FluentAssertions;

namespace Test.Invoices;

public sealed class ListInvoicesValidatorTests
{
    [Theory]
    [InlineData(1, 10, true)]
    [InlineData(1, 100, true)]
    [InlineData(0, 10, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    [InlineData(int.MaxValue, 100, false)]
    public async Task Validate_Pagination_EnforcesBounds(int page, int pageSize, bool expected)
    {
        var result = await new ListInvoicesValidator().ValidateAsync(
            new ListInvoicesRequest { Page = page, PageSize = pageSize }, TestContext.Current.CancellationToken);
        result.IsValid.Should().Be(expected);
    }
}
