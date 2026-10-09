namespace Application.Dtos;

/// <summary>
/// Modelo de salida del ejemplo DemoArchitecture.
/// </summary>
public sealed class DemoArchitectureDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
