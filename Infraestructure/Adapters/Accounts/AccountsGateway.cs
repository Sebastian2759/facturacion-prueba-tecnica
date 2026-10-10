using Domain.Contracts.Accounts;

namespace Adapters.Accounts;

public sealed class AccountsGateway(
    AccountIntegrationOptions options,
    NovasoftClient novasoft,
    SimulatedAccountsStore simulated) : IAccountsGateway
{
    public Task<IReadOnlyList<AccountRecord>> GetAsync(
        AccountExecutionMode mode, string subject, string? code, CancellationToken cancellationToken)
    {
        EnsureModeAllowed(mode);
        cancellationToken.ThrowIfCancellationRequested();
        return mode == AccountExecutionMode.Real
            ? novasoft.GetAsync(code, cancellationToken)
            : Task.FromResult(simulated.Get(subject, code));
    }

    public Task CreateAsync(
        AccountExecutionMode mode, string subject, AccountRecord account, CancellationToken cancellationToken)
    {
        EnsureModeAllowed(mode);
        cancellationToken.ThrowIfCancellationRequested();
        if (mode == AccountExecutionMode.Real)
            return novasoft.CreateAsync(account, cancellationToken);

        simulated.Create(subject, account);
        return Task.CompletedTask;
    }

    private void EnsureModeAllowed(AccountExecutionMode mode)
    {
        if (!Enum.IsDefined(mode))
            throw new AccountsIntegrationException("ACCOUNTS_MODE_INVALID", 400, "El modo solicitado no es válido.");
        if (mode == AccountExecutionMode.Real && !options.RealEnabled)
            throw new AccountsIntegrationException("ACCOUNTS_REAL_DISABLED", 403, "La integración real no está habilitada en este entorno.");
    }
}
