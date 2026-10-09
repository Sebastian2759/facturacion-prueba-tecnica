using Application.UseCases.Invoices.Common;
using Application.UseCases.Invoices.CreateInvoice;
using FluentAssertions;

namespace Test.Invoices;

public sealed class CreateInvoiceValidatorTests
{
    private readonly CreateInvoiceValidator _validator = new();

    [Fact]
    public async Task Validate_ValidCustomerAndDetails_Accepts()
    {
        var result = await _validator.ValidateAsync(InvoiceTestData.Request(), TestContext.Current.CancellationToken);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_RequiredCustomerFieldsAndHeader_ReturnsFieldFailures()
    {
        var result = await _validator.ValidateAsync(new CreateInvoiceRequest
        {
            Document = " ",
            Details = [new InvoiceLineInput { Code = 1, Quantity = 1, UnitPrice = 1 }],
        }, TestContext.Current.CancellationToken);

        result.Errors.Select(error => error.PropertyName).Should().Contain([
            "Document", "FirstName", "LastName", "Address", "Phone", "IdempotencyKey"]);
    }

    [Fact]
    public async Task Validate_LengthsAndIndexedDetail_ReturnsPrecisePaths()
    {
        var request = InvoiceTestData.Request() with
        {
            Document = new string('1', 16),
            FirstName = new string('A', 101),
            LastName = new string('A', 101),
            Address = new string('A', 201),
            Phone = new string('1', 31),
            Details = [new InvoiceLineInput { Code = 1, Quantity = 0, UnitPrice = 0.001m }],
        };

        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        result.Errors.Select(error => error.PropertyName).Should().Contain([
            "Document", "FirstName", "LastName", "Address", "Phone",
            "Details[0].Quantity", "Details[0].UnitPrice"]);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public async Task Validate_DetailCount_EnforcesLimit(int count, bool expected)
    {
        var request = InvoiceTestData.Request() with
        {
            Details = Enumerable.Repeat(new InvoiceLineInput { Code = 1, Quantity = 1, UnitPrice = 1 }, count).ToArray(),
        };

        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        result.IsValid.Should().Be(expected);
    }

    [Fact]
    public async Task Validate_NullDetail_DoesNotThrowAndRejects()
    {
        var request = InvoiceTestData.Request() with { Details = [null!] };
        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);
        result.IsValid.Should().BeFalse();
    }
}
