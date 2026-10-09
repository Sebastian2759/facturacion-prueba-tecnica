using System.Security.Cryptography;
using Api.Handlers;
using Api.Security;

namespace LocalEvaluation.Security;

internal static class AngularIndex
{
    private const string NoncePlaceholder = "__CSP_NONCE__";

    public static async Task RenderAsync(HttpContext context)
    {
        var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var file = environment.WebRootFileProvider.GetFileInfo("index.html");
        if (!file.Exists)
        {
            await ApiProblems.Write(context, 503, "La interfaz compilada no está disponible.", "frontend.unavailable");
            return;
        }

        await using var stream = file.CreateReadStream();
        using var reader = new StreamReader(stream);
        var template = await reader.ReadToEndAsync(context.RequestAborted);
        if (!template.Contains(NoncePlaceholder, StringComparison.Ordinal))
        {
            await ApiProblems.Write(context, 503, "La interfaz compilada no corresponde a este host.", "frontend.invalid");
            return;
        }

        // Only a fixed build-time placeholder is replaced; request values never enter the HTML.
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        context.Response.Headers.ContentSecurityPolicy = BrowserSecurityPolicy.WithStyleNonce(nonce);
        context.Response.Headers.CacheControl = "no-store";
        context.Response.ContentType = "text/html; charset=utf-8";
        if (!HttpMethods.IsHead(context.Request.Method))
            await context.Response.WriteAsync(template.Replace(NoncePlaceholder, nonce, StringComparison.Ordinal), context.RequestAborted);
    }
}
