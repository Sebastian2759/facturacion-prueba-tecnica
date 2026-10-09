using System.Security.Claims;

namespace Api.Security;

public static class Permissions
{
    public const string Evaluate = "invoices.evaluate";
    public const string RealAccounts = "accounts.real";

    public static bool Has(ClaimsPrincipal user, string permission)
        => user.Identity?.IsAuthenticated == true && user.Claims.Any(claim =>
            claim.Type is "roles" or "permissions" or ClaimTypes.Role &&
            string.Equals(claim.Value, permission, StringComparison.Ordinal));
}
