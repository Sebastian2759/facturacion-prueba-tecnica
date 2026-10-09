using Application.UseCases.Invoices.Common;
using Application.UseCases.Invoices.PreviewInvoice;
using Domain.Invoices;
using FluentAssertions;

namespace Test.Invoices;

public sealed class PreviewInvoiceValidatorTests
{
    [Fact]
    public async Task Validate_EmptyAndInvalidDetails_Rejects()
    {
        var validator = new PreviewInvoiceValidator();
        (await validator.ValidateAsync(new PreviewInvoiceRequest(), TestContext.Current.CancellationToken))
            .IsValid.Should().BeFalse();
        var invalid = await validator.ValidateAsync(new PreviewInvoiceRequest
        {
            Details = [new InvoiceLineInput { Code = 0, Quantity = -1, UnitPrice = 0 }],
        }, TestContext.Current.CancellationToken);
        invalid.Errors.Select(error => error.PropertyName).Should()
            .Contain(["Details[0].Code", "Details[0].Quantity", "Details[0].UnitPrice"]);
    }

    [Fact]
    public async Task Validate_ValidDetails_Accepts()
    {
        var result = await new PreviewInvoiceValidator().ValidateAsync(
            new PreviewInvoiceRequest { Details = InvoiceTestData.Request().Details },
            TestContext.Current.CancellationToken);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_PriceAboveApplicationCap_RejectsWithPriceField()
    {
        var validator = new PreviewInvoiceValidator();
        var request = new PreviewInvoiceRequest
        {
            Details = [new InvoiceLineInput { Code = 1, Quantity = 1, UnitPrice = InvoiceRules.MaximumMoney }],
        };
        (await validator.ValidateAsync(request, TestContext.Current.CancellationToken)).IsValid.Should().BeTrue();

        var oversized = request with
        {
            Details = [request.Details[0] with { UnitPrice = 1_000_000_000_000m }],
        };
        var result = await validator.ValidateAsync(oversized, TestContext.Current.CancellationToken);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Details[0].UnitPrice");
    }
}
