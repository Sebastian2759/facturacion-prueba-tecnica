using Application.Dtos;

namespace Application.UseCases.Invoices.GetInvoiceByNumber;

public sealed record class GetInvoiceByNumberResponse(InvoiceDetailDto Invoice);
