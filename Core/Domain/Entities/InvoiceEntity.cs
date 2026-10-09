using Domain.Base;

namespace Domain.Entities;

public sealed class InvoiceEntity : EntityBase
{
    public int Number { get; set; }
    public DateTime Date { get; set; }
    public required string OwnerSubject { get; set; }
    public Guid IdempotencyKey { get; set; }
    public required string RequestFingerprint { get; set; }
    public required string Document { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Address { get; set; }
    public required string Phone { get; set; }
    public decimal Gross { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public ICollection<InvoiceLineEntity> Details { get; set; } = [];
}
