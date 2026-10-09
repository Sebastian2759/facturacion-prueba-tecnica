using Application.Base;
using Application.Dtos;
using Application.UseCases.Invoices.Common;
using Domain.Contracts.Adapter.Mapper;
using Domain.Contracts.Persistence;
using Domain.Contracts.Security;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Invoices.ListInvoices;

public sealed class ListInvoicesQuery(IInvoiceRepository repository, IMapperAdapter mapper, ICurrentUser currentUser)
    : IRequestHandler<ListInvoicesRequest, ResponseBase<ListInvoicesResponse>>
{
    public async Task<ResponseBase<ListInvoicesResponse>> Handle(ListInvoicesRequest request, CancellationToken cancellationToken)
    {
        var owner = InvoiceAccess.RequireEvaluator(currentUser);
        var page = await repository.ListOwnedAsync(owner, request.Page, request.PageSize, cancellationToken);
        var items = page.Items.Select(invoice => mapper.Map<InvoiceEntity, InvoiceListDto>(invoice)).ToArray();
        return ResponseBase<ListInvoicesResponse>.Ok(
            new ListInvoicesResponse(items, request.Page, request.PageSize, page.TotalCount));
    }
}
