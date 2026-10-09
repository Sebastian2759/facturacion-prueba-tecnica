using Domain.Contracts.Security;

namespace Api.Session;

public sealed class WebSession(ICurrentUser user, IConfiguration configuration) : IHostSession
{
    public Task<SessionDto> GetAsync(HttpContext context) => Task.FromResult(new SessionDto(
        "online", context.User.Identity?.IsAuthenticated == true, user.CanEvaluate, user.CanUseRealAccounts,
        Oidc: new OidcSettings(configuration["Identity:Authority"]!, configuration["Identity:ClientId"]!,
            configuration["Identity:Scope"]!)));
}
