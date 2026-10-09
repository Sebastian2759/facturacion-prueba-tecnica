namespace Api.Security;

public static class BrowserSecurityPolicy
{
    private const string Directives = "default-src 'self'; script-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; object-src 'none'; form-action 'self'";

    public static string Default => $"{Directives}; style-src 'self'";

    public static string WithStyleNonce(string nonce) => $"{Directives}; style-src 'self' 'nonce-{nonce}'";
}
