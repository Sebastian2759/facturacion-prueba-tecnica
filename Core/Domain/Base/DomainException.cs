namespace Domain.Base;

/// <summary>
/// Excepción del dominio. Se usa para errores de lógica de negocio.
/// El GlobalExceptionHandler la captura y retorna 422 Unprocessable Entity.
/// </summary>
public sealed class DomainException : Exception
{
    public IReadOnlyDictionary<string, string> Errors { get; }

    public DomainException(DomainValidation validator)
        : base("Uno o más errores de dominio ocurrieron.")
    {
        Errors = validator.Fails;
    }

    public DomainException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string>();
    }

    public DomainException(string field, string message)
        : base(message)
    {
        Errors = new Dictionary<string, string> { [field] = message };
    }
}
