using Application.Base;
using MediatR;

namespace Application.UseCases.Invoices.GetInvoiceByNumber;

public sealed record class GetInvoiceByNumberRequest : IRequest<ResponseBase<GetInvoiceByNumberResponse>>
{
    public int Number { get; init; }
}
