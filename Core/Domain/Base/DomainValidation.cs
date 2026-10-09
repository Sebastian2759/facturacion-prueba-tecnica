namespace Domain.Base;

/// <summary>
/// Acumulador de errores de validación de dominio.
/// Uso: var v = new DomainValidation(); v.AddFailed("campo", "error");
/// if (!v.IsValid) throw new DomainException(v);
/// </summary>
public sealed class DomainValidation
{
    private readonly Dictionary<string, string> _fails = [];

    public IReadOnlyDictionary<string, string> Fails => _fails;

    public bool IsValid => _fails.Count == 0;

    public void AddFailed(string key, string error) => _fails.Add(key, error);
}
