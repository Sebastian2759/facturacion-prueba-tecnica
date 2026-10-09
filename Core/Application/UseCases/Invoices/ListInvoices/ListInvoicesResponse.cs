using Application.Dtos;

namespace Application.UseCases.Invoices.ListInvoices;

public sealed record class ListInvoicesResponse(
    IReadOnlyList<InvoiceListDto> Items, int Page, int PageSize, int TotalCount);
