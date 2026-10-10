using Domain.Contracts.Accounts;

namespace Adapters.Accounts;

public sealed class AccountsGateway(AccountIntegrationOptions options, NovasoftClient novasoft) : IAccountsGateway
{
    public Task<IReadOnlyList<AccountRecord>> GetAsync(string? code, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        cancellationToken.ThrowIfCancellationRequested();
        return novasoft.GetAsync(code, cancellationToken);
    }

    public Task CreateAsync(AccountRecord account, CancellationToken cancellationToken)
    {
        EnsureEnabled();
        cancellationToken.ThrowIfCancellationRequested();
        return novasoft.CreateAsync(account, cancellationToken);
    }

    private void EnsureEnabled()
    {
        if (!options.RealEnabled)
            throw new AccountsIntegrationException("ACCOUNTS_REAL_DISABLED", 403, "La conexión con Novasoft requiere configuración del servidor.");
    }
}
