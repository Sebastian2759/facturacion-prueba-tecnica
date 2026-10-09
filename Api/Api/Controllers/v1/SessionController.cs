using Api.Session;
using Application.Base;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/session")]
public sealed class SessionController(IHostSession session) : ApiControllerBase
{
    [HttpGet, AllowAnonymous]
    [ProducesResponseType(typeof(ResponseBase<SessionDto>), 200)]
    public async Task<IActionResult> Get() => ToResult(ResponseBase<SessionDto>.Ok(await session.GetAsync(HttpContext)));
}
