namespace Application.Dtos;

public sealed class InvoiceListDto
{
    public int Number { get; set; }
    public DateTime Date { get; set; }
    public string Document { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public decimal Total { get; set; }
}
