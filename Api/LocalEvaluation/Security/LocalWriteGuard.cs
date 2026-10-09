using Api.Handlers;
using Microsoft.AspNetCore.Antiforgery;

namespace LocalEvaluation.Security;

public sealed class LocalWriteGuard(RequestDelegate next, LocalSettings settings)
{
    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            await next(context);
            return;
        }
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await ApiProblems.Write(context, 401, "Inicie la sesión local.", "authentication.required");
            return;
        }
        if (!string.Equals(context.Request.Headers.Origin.ToString(), settings.Origin, StringComparison.Ordinal))
        {
            await ApiProblems.Write(context, 403, "La solicitud debe proceder de la aplicación local.", "local.origin_invalid");
            return;
        }
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            await ApiProblems.Write(context, 400, "Actualice la página para renovar la sesión local.", "csrf.invalid");
            return;
        }
        await next(context);
    }
}
