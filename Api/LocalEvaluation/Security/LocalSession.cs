using System.Security.Claims;
using Api.Security;
using Api.Session;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace LocalEvaluation.Security;

public sealed class LocalSession(LocalSettings settings, IAntiforgery antiforgery) : IHostSession
{
    public async Task<SessionDto> GetAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true ||
            context.User.FindFirst("sub")?.Value != settings.InstallationId)
        {
            var identity = new ClaimsIdentity(
            [
                new Claim("iss", "urn:facturacion:local"),
                new Claim("sub", settings.InstallationId),
                new Claim(ClaimTypes.NameIdentifier, settings.InstallationId),
                new Claim("roles", Permissions.Evaluate)
            ], CookieAuthenticationDefaults.AuthenticationScheme);
            context.User = new ClaimsPrincipal(identity);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, context.User);
        }
        var tokens = antiforgery.GetAndStoreTokens(context);
        return new SessionDto("local", true, true, false, tokens.RequestToken);
    }
}
