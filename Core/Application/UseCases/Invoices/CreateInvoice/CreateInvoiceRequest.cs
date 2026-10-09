using System.Text.Json.Serialization;
using Application.Base;
using Application.UseCases.Invoices.Common;
using MediatR;

namespace Application.UseCases.Invoices.CreateInvoice;

public sealed record class CreateInvoiceRequest : IRequest<ResponseBase<CreateInvoiceResponse>>
{
    public string Document { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public IReadOnlyList<InvoiceLineInput> Details { get; init; } = [];

    // Assigned by the HTTP boundary from Idempotency-Key, never from JSON.
    [JsonIgnore]
    public Guid IdempotencyKey { get; init; }
}
