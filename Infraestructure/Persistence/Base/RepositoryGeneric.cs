using System.Linq.Expressions;
using Domain.Base;
using Domain.Base.Interface;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Base;

/// <summary>
/// Implementación genérica del repositorio.
/// Add/Edit/Delete NO hacen SaveChanges; llama CommitAsync() para persistir.
/// </summary>
public class RepositoryGeneric<T>(AppDbContext context) : IRepositoryGeneric<T>
    where T : EntityBase
{
    protected AppDbContext Db { get; } = context;
    protected DbSet<T> DbSet { get; } = context.Set<T>();

    // --- Consultas ---

    public async Task<T?> FindAsync(Guid id, CancellationToken ct = default)
        => await DbSet.FindAsync([id], ct);

    public async Task<T?> FindFirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(predicate, ct);

    public Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken ct = default)
        => DbSet.AnyAsync(predicate, ct);

    // --- Comandos (sin commit automático) ---

    public void Add(T entity) => DbSet.Add(entity);

    public void AddRange(IEnumerable<T> entities) => DbSet.AddRange(entities);

    public void Edit(T entity) => Db.Entry(entity).State = EntityState.Modified;

    public void Delete(T entity) => DbSet.Remove(entity);

    public void DeleteRange(IEnumerable<T> entities) => DbSet.RemoveRange(entities);

    // --- Persistencia ---

    public Task<int> CommitAsync(CancellationToken ct = default)
        => Db.SaveChangesAsync(ct);
}
