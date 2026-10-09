namespace Domain.Contracts.Accounts;

/// <summary>Only stable, safe messages cross the provider boundary. No raw provider body or exception is retained.</summary>
public sealed class AccountsIntegrationException(string code, int httpStatus, string message) : Exception(message)
{
    public string Code { get; } = code;
    public int HttpStatus { get; } = httpStatus;
}
