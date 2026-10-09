using Api.Security;
using Application.Base;
using Application.UseCases.Accounts.CreateAccount;
using Application.UseCases.Accounts.GetAccounts;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/accounts")]
[Authorize(Policy = Permissions.Evaluate)]
public sealed class AccountsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ResponseBase<GetAccountsResponse>), 200)]
    public async Task<IActionResult> Get([FromQuery] GetAccountsRequest request, CancellationToken ct)
        => ToResult(await mediator.Send(request, ct));

    [HttpPost]
    [ProducesResponseType(typeof(ResponseBase<CreateAccountResponse>), 201)]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request, CancellationToken ct)
        => ToResult(await mediator.Send(request, ct));
}
