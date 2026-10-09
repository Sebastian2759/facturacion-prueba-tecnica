using Application.Base;
using Application.UseCases.Invoices.Common;
using Domain.Contracts.Security;
using MediatR;

namespace Application.UseCases.Invoices.PreviewInvoice;

public sealed class PreviewInvoiceQuery(ICurrentUser currentUser)
    : IRequestHandler<PreviewInvoiceRequest, ResponseBase<PreviewInvoiceResponse>>
{
    public Task<ResponseBase<PreviewInvoiceResponse>> Handle(PreviewInvoiceRequest request, CancellationToken cancellationToken)
    {
        InvoiceAccess.RequireEvaluator(currentUser);
        cancellationToken.ThrowIfCancellationRequested();
        var calculation = InvoiceInputCalculation.Calculate(request.Details);
        return Task.FromResult(ResponseBase<PreviewInvoiceResponse>.Ok(new PreviewInvoiceResponse(
            calculation.Gross, calculation.Discount, calculation.Tax, calculation.Total)));
    }
}
