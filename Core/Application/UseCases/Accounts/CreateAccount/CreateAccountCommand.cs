using Application.Base;
using Domain.Contracts.Accounts;
using Domain.Contracts.Security;
using MediatR;

namespace Application.UseCases.Accounts.CreateAccount;

public sealed class CreateAccountCommand(IAccountsGateway gateway, ICurrentUser currentUser)
    : IRequestHandler<CreateAccountRequest, ResponseBase<CreateAccountResponse>>
{
    public async Task<ResponseBase<CreateAccountResponse>> Handle(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var mode = AccountAccess.Authorize(request.Mode, currentUser);
        var account = new AccountRecord(
            request.Code, request.IdentificationType, request.Document, request.Name,
            request.FirstName, request.LastName, request.Email, request.PersonType,
            request.CityCode, request.DepartmentCode, request.CountryCode, request.Address,
            request.AdditionalAddress, request.Phone, request.CustomerType,
            request.Website, request.RegisteredAt, request.ExternalCode);
        await gateway.CreateAsync(mode, currentUser.Subject, account, cancellationToken);
        return ResponseBase<CreateAccountResponse>.Created(new(request.Mode, request.Code, true), "Cuenta aceptada. Puede consultar su código para verificarla.");
    }
}
