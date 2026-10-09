using Api.Handlers;

namespace LocalEvaluation.Security;

public sealed class LocalHostGuard(RequestDelegate next, LocalSettings settings)
{
    public Task InvokeAsync(HttpContext context)
    {
        var host = new Uri(settings.Origin).Authority;
        if (!string.Equals(context.Request.Host.Value, host, StringComparison.OrdinalIgnoreCase) ||
            context.Request.Headers.ContainsKey("X-Forwarded-Host") ||
            context.Request.Headers.ContainsKey("Forwarded"))
            return ApiProblems.Write(context, 400, "Origen local no válido.", "local.host_invalid");
        return next(context);
    }
}
