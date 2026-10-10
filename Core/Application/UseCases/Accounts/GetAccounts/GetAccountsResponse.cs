using Application.Dtos;

namespace Application.UseCases.Accounts.GetAccounts;

public sealed record GetAccountsResponse(IReadOnlyList<AccountDto> Accounts);
