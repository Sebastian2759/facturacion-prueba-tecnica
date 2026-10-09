using Domain.Base.Interface;
using Domain.Entities;

namespace Domain.Contracts.Persistence;

public sealed record InvoicePage(IReadOnlyList<InvoiceEntity> Items, int TotalCount);
public sealed record InvoiceCreateResult(InvoiceEntity Invoice, bool WasCreated);

public interface IInvoiceRepository : IRepositoryGeneric<InvoiceEntity>
{
    Task<InvoiceEntity?> FindOwnedByNumberAsync(string ownerSubject, int number, CancellationToken ct = default);
    Task<InvoiceEntity?> FindOwnedByIdempotencyKeyAsync(string ownerSubject, Guid key, CancellationToken ct = default);
    Task<InvoicePage> ListOwnedAsync(string ownerSubject, int page, int pageSize, CancellationToken ct = default);
    Task<InvoiceCreateResult> CreateOrGetAsync(InvoiceEntity invoice, CancellationToken ct = default);
}
