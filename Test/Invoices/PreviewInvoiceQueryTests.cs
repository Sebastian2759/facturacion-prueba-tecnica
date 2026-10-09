using Application.UseCases.Invoices.Common;
using Application.UseCases.Invoices.PreviewInvoice;
using Domain.Base;
using FluentAssertions;

namespace Test.Invoices;

public sealed class PreviewInvoiceQueryTests
{
    [Fact]
    public async Task Handle_ValidPreview_ComputesWithoutRepository()
    {
        var query = new PreviewInvoiceQuery(new InvoiceTestUser());
        var result = await query.Handle(new PreviewInvoiceRequest
        {
            Details = [new InvoiceLineInput { Code = 1, Quantity = 2, UnitPrice = 250_000m }],
        }, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(new PreviewInvoiceResponse(500_000m, 25_000m, 90_250m, 565_250m));
    }

    [Fact]
    public async Task Handle_WithoutPermission_Rejects()
    {
        var query = new PreviewInvoiceQuery(new InvoiceTestUser(canEvaluate: false));
        Func<Task> action = () => query.Handle(new PreviewInvoiceRequest(), TestContext.Current.CancellationToken);
        await action.Should().ThrowAsync<AccessDeniedException>();
    }
}
