using Application.Dtos;

namespace Application.UseCases.Accounts.GetAccounts;

public sealed record GetAccountsResponse(string Mode, IReadOnlyList<AccountDto> Accounts);
