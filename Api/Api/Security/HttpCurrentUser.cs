using System.Security.Cryptography;
using System.Text;
using Domain.Base;
using Domain.Contracts.Security;

namespace Api.Security;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string Subject
    {
        get
        {
            var user = accessor.HttpContext?.User;
            var issuer = user?.FindFirst("iss")?.Value;
            var subject = user?.FindFirst("sub")?.Value;
            if (user?.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(subject))
                throw new AccessDeniedException();

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{issuer.Length}:{issuer}{subject}")));
        }
    }

    public bool CanEvaluate => accessor.HttpContext is { } context && Permissions.Has(context.User, Permissions.Evaluate);
    public bool CanUseRealAccounts => accessor.HttpContext is { } context && Permissions.Has(context.User, Permissions.RealAccounts);
}
