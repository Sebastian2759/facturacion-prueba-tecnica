namespace Adapters.Accounts;

/// <summary>Bind only from server secrets. Never serialize or log this object.</summary>
public sealed class NovasoftOptions
{
    public string UserLogin { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ConnectionName { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(UserLogin) &&
        !string.IsNullOrWhiteSpace(Password) &&
        !string.IsNullOrWhiteSpace(ConnectionName);
}
