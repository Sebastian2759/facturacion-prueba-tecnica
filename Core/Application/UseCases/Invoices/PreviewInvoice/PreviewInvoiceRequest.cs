using Application.Base;
using Application.UseCases.Invoices.Common;
using MediatR;

namespace Application.UseCases.Invoices.PreviewInvoice;

public sealed record class PreviewInvoiceRequest : IRequest<ResponseBase<PreviewInvoiceResponse>>
{
    public IReadOnlyList<InvoiceLineInput> Details { get; init; } = [];
}
