using Application.Base;
using MediatR;

namespace Application.UseCases.Invoices.ListInvoices;

public sealed record class ListInvoicesRequest : IRequest<ResponseBase<ListInvoicesResponse>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
