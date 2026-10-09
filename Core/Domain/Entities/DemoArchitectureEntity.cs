using Domain.Base;

namespace Domain.Entities;

/// <summary>
/// Entidad de ejemplo que muestra la convención de entidades persistidas.
/// Debe reemplazarse por una entidad del negocio al crear una solución real.
/// </summary>
public sealed class DemoArchitectureEntity : EntityBase
{
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
}
