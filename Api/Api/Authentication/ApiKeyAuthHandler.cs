using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Api.Authentication;

/// <summary>
/// Handler de autenticación por API Key (opción 1 recomendada).
///
/// Integra con el pipeline de ASP.NET Core:
/// - Usa [Authorize(AuthenticationSchemes = ApiKeyDefaults.SchemeName)]
/// - Aparece en OpenAPI como securityScheme
/// - Coexiste con JWT Bearer (multi-scheme)
///
/// Header esperado: X-Api-Key: {tu-clave}
/// Configuración:  appsettings.json → "ApiKey": { "Key": "mi-clave-secreta", "HeaderName": "X-Api-Key" }
/// </summary>
public sealed class ApiKeyAuthHandler(
    IOptionsMonitor<ApiKeyOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<ApiKeyOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrEmpty(Options.Key) || Options.Key.Length < 32 ||
            !Request.Headers.TryGetValue(Options.HeaderName, out var keyValue))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (keyValue.Count != 1 || keyValue.ToString().Length > 256 ||
            !CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(keyValue.ToString())),
                SHA256.HashData(Encoding.UTF8.GetBytes(Options.Key))))
            return Task.FromResult(AuthenticateResult.Fail("API Key inválida."));

        var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyClient") };
        var identity = new ClaimsIdentity(claims, ApiKeyDefaults.SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyDefaults.SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public static class ApiKeyDefaults
{
    public const string SchemeName = "ApiKey";
}
