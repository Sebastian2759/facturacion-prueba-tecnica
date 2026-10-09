namespace Domain.Contracts.Accounts;

public interface IAccountsGateway
{
    Task<IReadOnlyList<AccountRecord>> GetAsync(
        AccountExecutionMode mode, string subject, string? code, CancellationToken cancellationToken);

    Task CreateAsync(
        AccountExecutionMode mode, string subject, AccountRecord account, CancellationToken cancellationToken);
}
