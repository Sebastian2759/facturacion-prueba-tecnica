using Application.UseCases.Invoices.Common;
using Application.UseCases.Invoices.CreateInvoice;
using Domain.Contracts.Security;
using Domain.Entities;
using Domain.Invoices;

namespace Test.Invoices;

internal static class InvoiceTestData
{
    public static CreateInvoiceRequest Request(Guid? key = null) => new()
    {
        Document = "123456789",
        FirstName = "Ana",
        LastName = "Ejemplo",
        Address = "Calle ficticia 1",
        Phone = "3000000000",
        IdempotencyKey = key ?? Guid.NewGuid(),
        Details = [new InvoiceLineInput { Code = 10, Quantity = 2, UnitPrice = 250_000m }],
    };

    public static InvoiceEntity Entity(string owner = "owner-a", Guid? key = null)
    {
        var calculation = InvoiceCalculator.Calculate([new InvoiceLineValue(10, 2, 250_000m)]);
        var invoice = new InvoiceEntity
        {
            OwnerSubject = owner,
            IdempotencyKey = key ?? Guid.NewGuid(),
            RequestFingerprint = new string('A', 64),
            Date = new DateTime(2026, 10, 9),
            Document = "123456789",
            FirstName = "Ana",
            LastName = "Ejemplo",
            Address = "Calle ficticia 1",
            Phone = "3000000000",
            Gross = calculation.Gross,
            Discount = calculation.Discount,
            Tax = calculation.Tax,
            Total = calculation.Total,
            CreatedAtUtc = new DateTime(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2026, 10, 9, 12, 0, 0, DateTimeKind.Utc),
        };
        invoice.Details = calculation.Details.Select((line, index) => new InvoiceLineEntity
        {
            InvoiceId = invoice.Id,
            Position = index,
            Code = line.Code,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            LineTotal = line.LineTotal,
            CreatedAtUtc = invoice.CreatedAtUtc,
            UpdatedAtUtc = invoice.UpdatedAtUtc,
        }).ToList();
        return invoice;
    }
}

internal sealed class InvoiceTestUser(string subject = "owner-a", bool canEvaluate = true) : ICurrentUser
{
    public string Subject => subject;
    public bool CanEvaluate => canEvaluate;
    public bool CanUseRealAccounts => false;
}

internal sealed class InvoiceTestTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
