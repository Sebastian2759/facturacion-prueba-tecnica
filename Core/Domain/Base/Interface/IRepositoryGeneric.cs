using System.Linq.Expressions;

namespace Domain.Base.Interface;

/// <summary>
/// Contrato genérico de repositorio. Los repositorios concretos heredan de este.
/// Nota: Add/Edit/Delete NO hacen commit inmediato; llama CommitAsync() al finalizar.
/// </summary>
public interface IRepositoryGeneric<T> where T : EntityBase
{
    // --- Consultas ---
    Task<T?> FindAsync(Guid id, CancellationToken ct = default);
    Task<T?> FindFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    // --- Comandos (sin commit automático) ---
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void Edit(T entity);
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);

    // --- Persistencia ---
    Task<int> CommitAsync(CancellationToken ct = default);
}
