namespace Application.UseCases.Accounts.CreateAccount;

public sealed record CreateAccountResponse(string Mode, string Code, bool Accepted);
