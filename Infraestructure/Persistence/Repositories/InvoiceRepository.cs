using Domain.Base;
using Domain.Contracts.Persistence;
using Domain.Entities;
using Domain.Invoices;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Persistence.Base;
using Persistence.Context;

namespace Persistence.Repositories;

public sealed class InvoiceRepository(AppDbContext context)
    : RepositoryGeneric<InvoiceEntity>(context), IInvoiceRepository
{
    public Task<InvoiceEntity?> FindOwnedByNumberAsync(string ownerSubject, int number, CancellationToken ct = default)
        => Owned(ownerSubject).Include(invoice => invoice.Details.OrderBy(line => line.Position))
            .SingleOrDefaultAsync(invoice => invoice.Number == number, ct);

    public Task<InvoiceEntity?> FindOwnedByIdempotencyKeyAsync(string ownerSubject, Guid key, CancellationToken ct = default)
        => Owned(ownerSubject).Include(invoice => invoice.Details.OrderBy(line => line.Position))
            .SingleOrDefaultAsync(invoice => invoice.IdempotencyKey == key, ct);

    public async Task<InvoicePage> ListOwnedAsync(string ownerSubject, int page, int pageSize, CancellationToken ct = default)
    {
        if (page is < 1 or > 1_000_000 || pageSize is < 1 or > 100)
            throw new DomainException("page", "La paginación está fuera del rango permitido.");

        var query = Owned(ownerSubject);
        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(invoice => invoice.Number)
            .Skip(checked((page - 1) * pageSize)).Take(pageSize).ToListAsync(ct);
        return new InvoicePage(items, totalCount);
    }

    public async Task<InvoiceCreateResult> CreateOrGetAsync(InvoiceEntity invoice, CancellationToken ct = default)
    {
        EnsureOwner(invoice.OwnerSubject);
        if (invoice.IdempotencyKey == Guid.Empty)
            throw new DomainException("idempotencyKey", "La clave de idempotencia es obligatoria.");

        // Invoice and all details are inserted by one SaveChanges, whose relational
        // transaction is atomic. SQL's unique owner/key index resolves concurrent
        // requests and ambiguous retries without duplicating the aggregate.
        Add(invoice);
        try
        {
            await CommitAsync(ct);
            return new InvoiceCreateResult(invoice, true);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            foreach (var line in invoice.Details.ToArray())
                Db.Entry(line).State = EntityState.Detached;
            Db.Entry(invoice).State = EntityState.Detached;

            var existing = await FindOwnedByIdempotencyKeyAsync(
                invoice.OwnerSubject, invoice.IdempotencyKey, ct);
            if (existing is null)
                throw;

            return new InvoiceCreateResult(existing, false);
        }
    }

    private IQueryable<InvoiceEntity> Owned(string ownerSubject)
    {
        EnsureOwner(ownerSubject);
        return DbSet.AsNoTracking().Where(invoice => invoice.OwnerSubject == ownerSubject);
    }

    private static void EnsureOwner(string ownerSubject)
    {
        if (string.IsNullOrWhiteSpace(ownerSubject) || ownerSubject.Length > InvoiceRules.OwnerMaxLength)
            throw new AccessDeniedException();
    }
}
