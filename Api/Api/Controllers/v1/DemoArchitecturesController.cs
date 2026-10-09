using Application.Base;
using Application.UseCases.DemoArchitectures.GetDemoArchitectureById;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

[ApiVersion("1.0")]
[NonController]
[Route("api/v{version:apiVersion}/demo-architectures")]
[Authorize(Policy = "BearerOrApiKey")]
public sealed class DemoArchitecturesController(IMediator mediator) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(ResponseBase<GetDemoArchitectureByIdResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ResponseBase<GetDemoArchitectureByIdResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(
        [FromQuery] GetDemoArchitectureByIdRequest request,
        CancellationToken cancellationToken)
        => ToResult(await mediator.Send(request, cancellationToken));
}
