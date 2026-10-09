namespace Application.Dtos;

public sealed class InvoiceLineDto
{
    public int Code { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
