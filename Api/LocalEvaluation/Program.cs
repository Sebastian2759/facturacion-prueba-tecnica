using Api.Bootstrap;
using Api.Handlers;
using Api.Session;
using LocalEvaluation.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
// A private Docker secret is optional. Never ship provider credentials in the ZIP.
builder.Configuration.AddJsonFile("/run/secrets/novasoft", optional: true, reloadOnChange: false);
var origin = builder.Configuration["Local:Origin"] ?? "http://127.0.0.1:5080";
if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri) || originUri.Scheme != "http" ||
    originUri.Host != "127.0.0.1" || originUri.AbsolutePath != "/" ||
    !string.IsNullOrEmpty(originUri.Query) || !string.IsNullOrEmpty(originUri.Fragment))
    throw new InvalidOperationException("Local:Origin debe ser un origen HTTP de 127.0.0.1.");
if (!Guid.TryParse(builder.Configuration["Local:InstallationId"], out var installationId) || installationId == Guid.Empty)
    throw new InvalidOperationException("Local:InstallationId es obligatorio. Use el comando de inicio.");

var inContainer = string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);
builder.WebHost.UseUrls(inContainer ? "http://0.0.0.0:8080" : originUri.GetLeftPart(UriPartial.Authority));
builder.Services.AddSingleton(new LocalSettings(originUri.GetLeftPart(UriPartial.Authority), installationId.ToString("D")));
ApiServices.AddCore(builder, isLocal: true);
builder.Services.AddScoped<IHostSession, LocalSession>();
builder.Services.AddDataProtection().SetApplicationName($"Facturacion.LocalEvaluation.{installationId:N}")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Path.GetTempPath(), "facturacion-session-keys", installationId.ToString("N"))));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = $"Facturacion.LocalSession.{installationId:N}";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = false;
    options.Events.OnValidatePrincipal = async context =>
    {
        if (context.Principal?.FindFirst("iss")?.Value != "urn:facturacion:local" ||
            context.Principal.FindFirst("sub")?.Value != installationId.ToString("D"))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    };
    options.Events.OnRedirectToLogin = context => ApiProblems.Write(context.HttpContext, 401,
        "Abra la aplicación local para iniciar la sesión.", "authentication.required");
    options.Events.OnRedirectToAccessDenied = context => ApiProblems.Write(context.HttpContext, 403,
        "No tiene permiso para esta operación.", "access.denied");
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = $"Facturacion.Antiforgery.{installationId:N}";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});

var app = builder.Build();
app.UseMiddleware<LocalHostGuard>();
ApiServices.UseCore(app, isLocal: true, additionalSecurity: host => host.UseMiddleware<LocalWriteGuard>());
app.MapFallback("/api/{**path}", context => ApiProblems.Write(context, 404,
    "No se encontró el recurso solicitado.", "resource.not_found")).AllowAnonymous();
app.MapMethods("/index.html", ["GET", "HEAD"], AngularIndex.RenderAsync).AllowAnonymous();
app.MapFallback(AngularIndex.RenderAsync).WithMetadata(new HttpMethodMetadata(["GET", "HEAD"])).AllowAnonymous();
app.Run();

namespace LocalEvaluation
{
    public partial class LocalProgram { }
}
