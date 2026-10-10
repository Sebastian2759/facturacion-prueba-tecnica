namespace Domain.Contracts.Accounts;

public interface IAccountsGateway
{
    Task<IReadOnlyList<AccountRecord>> GetAsync(string? code, CancellationToken cancellationToken);
    Task CreateAsync(AccountRecord account, CancellationToken cancellationToken);
}
