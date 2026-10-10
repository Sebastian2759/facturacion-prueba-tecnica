using Application.Base;
using Application.Dtos;
using Domain.Contracts.Accounts;
using Domain.Contracts.Security;
using MediatR;

namespace Application.UseCases.Accounts.GetAccounts;

public sealed class GetAccountsQuery(IAccountsGateway gateway, ICurrentUser currentUser)
    : IRequestHandler<GetAccountsRequest, ResponseBase<GetAccountsResponse>>
{
    public async Task<ResponseBase<GetAccountsResponse>> Handle(GetAccountsRequest request, CancellationToken cancellationToken)
    {
        AccountAccess.Authorize(currentUser);
        var accounts = await gateway.GetAsync(request.Code, cancellationToken);
        return ResponseBase<GetAccountsResponse>.Ok(new(accounts.Select(AccountDto.FromRecord).ToArray()));
    }
}
