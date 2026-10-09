namespace Domain.Base;

/// <summary>
/// Base para todas las entidades del dominio.
/// Incluye campos de auditoría estándar.
/// </summary>
public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}
