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
        var mode = AccountAccess.Authorize(request.Mode, currentUser);
        var accounts = await gateway.GetAsync(mode, currentUser.Subject, request.Code, cancellationToken);
        return ResponseBase<GetAccountsResponse>.Ok(new(request.Mode, accounts.Select(AccountDto.FromRecord).ToArray()));
    }
}
