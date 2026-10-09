using Application.Base;
using Application.Dtos;
using Application.UseCases.Invoices.Common;
using Domain.Contracts.Adapter.Mapper;
using Domain.Contracts.Persistence;
using Domain.Contracts.Security;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Invoices.CreateInvoice;

public sealed class CreateInvoiceCommand(
    IInvoiceRepository repository,
    IMapperAdapter mapper,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    TimeZoneInfo businessTimeZone)
    : IRequestHandler<CreateInvoiceRequest, ResponseBase<CreateInvoiceResponse>>
{
    public async Task<ResponseBase<CreateInvoiceResponse>> Handle(CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var owner = InvoiceAccess.RequireEvaluator(currentUser);
        var normalized = request with
        {
            Document = request.Document.Trim(),
            FirstName = request.FirstName?.Trim() ?? string.Empty,
            LastName = request.LastName?.Trim() ?? string.Empty,
            Address = request.Address?.Trim() ?? string.Empty,
            Phone = request.Phone?.Trim() ?? string.Empty,
        };
        var calculation = InvoiceInputCalculation.Calculate(normalized.Details);
        var fingerprint = InvoiceRequestFingerprint.Calculate(normalized);
        var existing = await repository.FindOwnedByIdempotencyKeyAsync(owner, request.IdempotencyKey, cancellationToken);
        if (existing is not null)
            return ExistingResult(existing, fingerprint);

        var now = timeProvider.GetUtcNow();
        var invoice = new InvoiceEntity
        {
            OwnerSubject = owner,
            IdempotencyKey = request.IdempotencyKey,
            RequestFingerprint = fingerprint,
            Date = TimeZoneInfo.ConvertTime(now, businessTimeZone).Date,
            Document = normalized.Document,
            FirstName = normalized.FirstName,
            LastName = normalized.LastName,
            Address = normalized.Address,
            Phone = normalized.Phone,
            Gross = calculation.Gross,
            Discount = calculation.Discount,
            Tax = calculation.Tax,
            Total = calculation.Total,
            CreatedAtUtc = now.UtcDateTime,
            UpdatedAtUtc = now.UtcDateTime,
        };
        invoice.Details = calculation.Details.Select((line, index) => new InvoiceLineEntity
        {
            InvoiceId = invoice.Id,
            Position = index,
            Code = line.Code,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            LineTotal = line.LineTotal,
            CreatedAtUtc = now.UtcDateTime,
            UpdatedAtUtc = now.UtcDateTime,
        }).ToList();

        var result = await repository.CreateOrGetAsync(invoice, cancellationToken);
        if (!result.WasCreated)
            return ExistingResult(result.Invoice, fingerprint);

        return ResponseBase<CreateInvoiceResponse>.Created(
            new CreateInvoiceResponse(mapper.Map<InvoiceEntity, InvoiceDetailDto>(result.Invoice)));
    }

    private ResponseBase<CreateInvoiceResponse> ExistingResult(InvoiceEntity invoice, string fingerprint)
    {
        if (!string.Equals(invoice.RequestFingerprint, fingerprint, StringComparison.Ordinal))
            return ResponseBase<CreateInvoiceResponse>.Conflict(
                "La clave de idempotencia ya se utilizó con una solicitud diferente.");

        return ResponseBase<CreateInvoiceResponse>.Ok(
            new CreateInvoiceResponse(mapper.Map<InvoiceEntity, InvoiceDetailDto>(invoice)),
            "La factura ya estaba registrada; no se creó un duplicado.");
    }
}
