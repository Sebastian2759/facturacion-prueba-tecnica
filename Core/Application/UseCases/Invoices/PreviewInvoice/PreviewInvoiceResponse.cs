namespace Application.UseCases.Invoices.PreviewInvoice;

public sealed record class PreviewInvoiceResponse(decimal Gross, decimal Discount, decimal Tax, decimal Total);
