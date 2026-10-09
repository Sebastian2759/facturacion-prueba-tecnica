using Application.Base;
using MediatR;

namespace Application.UseCases.Accounts.GetAccounts;

public sealed class GetAccountsRequest : IRequest<ResponseBase<GetAccountsResponse>>
{
    public string Mode { get; init; } = "simulated";
    public string? Code { get; init; }
}
