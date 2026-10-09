using Domain.Base;

namespace Domain.Entities;

public sealed class InvoiceLineEntity : EntityBase
{
    public Guid InvoiceId { get; set; }
    public InvoiceEntity Invoice { get; set; } = null!;
    public int Position { get; set; }
    public int Code { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
