using Application.Base;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Base para todos los controladores.
/// Centraliza la conversión de ResponseBase&lt;T&gt; → IActionResult.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ToResult<T>(ResponseBase<T> response)
        => StatusCode((int)response.StatusCode,
            response.WithTraceId(HttpContext?.TraceIdentifier ?? string.Empty));
}
