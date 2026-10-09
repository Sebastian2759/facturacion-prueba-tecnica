namespace Api.Session;

public sealed record OidcSettings(string Authority, string ClientId, string Scope);
public sealed record SessionDto(string Mode, bool Authenticated, bool CanEvaluate, bool CanUseRealAccounts,
    string? CsrfToken = null, OidcSettings? Oidc = null);

public interface IHostSession
{
    Task<SessionDto> GetAsync(HttpContext context);
}
