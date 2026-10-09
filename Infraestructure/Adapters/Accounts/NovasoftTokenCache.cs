using Domain.Contracts.Accounts;

namespace Adapters.Accounts;

/// <summary>A single bounded token cache for the configured service identity; login requests are serialized.</summary>
public sealed class NovasoftTokenCache(TimeProvider timeProvider) : IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private string? token;
    private DateTimeOffset reusableUntil;

    public async Task<string> GetAsync(
        Func<CancellationToken, Task<(string Token, DateTimeOffset Expiration)>> login,
        CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (token is not null && timeProvider.GetUtcNow() < reusableUntil)
                return token;

            token = null;
            var response = await login(cancellationToken);
            var now = timeProvider.GetUtcNow();
            var safeExpiration = response.Expiration - TimeSpan.FromSeconds(30);
            if (string.IsNullOrWhiteSpace(response.Token) || safeExpiration <= now)
                throw new AccountsIntegrationException("PROVIDER_INVALID_TOKEN", 502, "El proveedor no entregó una sesión válida.");

            reusableUntil = safeExpiration < now.AddMinutes(5) ? safeExpiration : now.AddMinutes(5);
            token = response.Token;
            return token;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task InvalidateAsync(string rejectedToken, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (string.Equals(token, rejectedToken, StringComparison.Ordinal))
                token = null;
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose() => gate.Dispose();
}
