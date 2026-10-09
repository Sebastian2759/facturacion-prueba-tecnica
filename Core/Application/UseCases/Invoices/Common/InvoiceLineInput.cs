namespace Application.UseCases.Invoices.Common;

public sealed record class InvoiceLineInput
{
    public int Code { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}
