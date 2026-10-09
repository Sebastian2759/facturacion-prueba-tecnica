namespace Domain.Base;

public sealed class AccessDeniedException : Exception
{
    public AccessDeniedException() : base("No tiene permiso para esta operación.") { }
}
