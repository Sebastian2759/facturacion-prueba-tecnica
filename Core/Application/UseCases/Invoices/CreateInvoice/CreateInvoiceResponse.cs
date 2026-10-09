using Application.Dtos;

namespace Application.UseCases.Invoices.CreateInvoice;

public sealed record class CreateInvoiceResponse(InvoiceDetailDto Invoice);
