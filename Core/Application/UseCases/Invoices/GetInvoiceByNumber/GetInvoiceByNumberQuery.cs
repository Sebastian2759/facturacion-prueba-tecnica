using Application.Base;
using Application.Dtos;
using Application.UseCases.Invoices.Common;
using Domain.Contracts.Adapter.Mapper;
using Domain.Contracts.Persistence;
using Domain.Contracts.Security;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Invoices.GetInvoiceByNumber;

public sealed class GetInvoiceByNumberQuery(IInvoiceRepository repository, IMapperAdapter mapper, ICurrentUser currentUser)
    : IRequestHandler<GetInvoiceByNumberRequest, ResponseBase<GetInvoiceByNumberResponse>>
{
    public async Task<ResponseBase<GetInvoiceByNumberResponse>> Handle(GetInvoiceByNumberRequest request, CancellationToken cancellationToken)
    {
        var owner = InvoiceAccess.RequireEvaluator(currentUser);
        var invoice = await repository.FindOwnedByNumberAsync(owner, request.Number, cancellationToken);
        return invoice is null
            ? ResponseBase<GetInvoiceByNumberResponse>.NotFound("Factura no encontrada.")
            : ResponseBase<GetInvoiceByNumberResponse>.Ok(
                new GetInvoiceByNumberResponse(mapper.Map<InvoiceEntity, InvoiceDetailDto>(invoice)));
    }
}
