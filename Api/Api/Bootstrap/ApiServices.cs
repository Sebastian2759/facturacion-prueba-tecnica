using System.Threading.RateLimiting;
using Adapters;
using Adapters.Accounts;
using Api.Handlers;
using Api.Security;
using Application;
using Asp.Versioning.Conventions;
using Domain.Contracts.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.OpenApi;
using Persistence;

namespace Api.Bootstrap;

public static class ApiServices
{
    private static readonly string[] InvalidValueMessages = ["El valor proporcionado no es válido."];
    public static void AddCore(WebApplicationBuilder builder, bool isLocal)
    {
        builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 256 * 1024);
        builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.None);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
        // Framework request logs include query strings (for example an account code).
        builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
        builder.Services.AddControllers().AddApplicationPart(typeof(ApiServices).Assembly)
            .AddJsonOptions(options => options.JsonSerializerOptions.MaxDepth = 16);
        builder.Services.Configure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = context =>
        {
            var fields = context.ModelState.Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(entry => ApiProblems.JsonPath(entry.Key), _ => InvalidValueMessages);
            return new BadRequestObjectResult(ApiProblems.Create(context.HttpContext, 400,
                "Revise los campos indicados.", "validation.failed", fields));
        });
        builder.Services.AddApiVersioning(options => options.ReportApiVersions = true)
            .AddMvc(options => options.Conventions.Add(new VersionByNamespaceConvention()))
            .AddApiExplorer(options => { options.GroupNameFormat = "'v'VVV"; options.SubstituteApiVersionInUrl = true; });
        builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "Facturación · API",
                Version = "v1",
                Description = "Facturas propias, cálculo autoritativo y cuentas integradas con Novasoft."
            };
            return Task.CompletedTask;
        }));

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById("America/Bogota"));
        builder.Services.AddAdapterServices();
        builder.Services.AddApplicationServices();
        builder.Services.AddPersistenceServices(builder.Configuration);
        var providerOptions = builder.Configuration.GetSection("Novasoft").Get<NovasoftOptions>() ?? new();
        var realEnabled = builder.Configuration.GetValue<bool>("Novasoft:Enabled");
        if (realEnabled && !providerOptions.IsConfigured)
            throw new InvalidOperationException("Novasoft requiere configuración privada completa antes de habilitar el modo real.");
        builder.Services.AddSingleton(new AccountIntegrationOptions
        {
            RealEnabled = realEnabled
        });
        builder.Services.AddSingleton(providerOptions);
        builder.Services.AddSingleton<NovasoftTokenCache>();
        builder.Services.AddHttpClient<NovasoftClient>(client =>
        {
            client.BaseAddress = NovasoftClient.BaseAddress;
            client.Timeout = TimeSpan.FromSeconds(20);
            client.MaxResponseContentBufferSize = 2 * 1024 * 1024;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.AddPolicy(Permissions.Evaluate, policy => policy.RequireAssertion(context => Permissions.Has(context.User, Permissions.Evaluate)));
            options.AddPolicy(Permissions.RealAccounts, policy => policy.RequireAssertion(context => Permissions.Has(context.User, Permissions.RealAccounts)));
            // Legacy template policies remain available; user resources never accept API keys.
            options.AddPolicy("JwtOnly", policy => policy.RequireAuthenticatedUser());
            options.AddPolicy("ApiKeyOnly", policy => policy.AddAuthenticationSchemes("ApiKey").RequireAuthenticatedUser());
            options.AddPolicy("BearerOrApiKey", policy => policy.AddAuthenticationSchemes("Bearer", "ApiKey").RequireAuthenticatedUser());
        });
        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var write = !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method);
                var subject = context.User.Identity?.IsAuthenticated == true
                    ? context.User.FindFirst("sub")?.Value : context.Connection.RemoteIpAddress?.ToString();
                var key = $"{(write ? "write" : "read")}:{subject ?? "unknown"}";
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = write ? 30 : 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
            options.OnRejected = async (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await ApiProblems.Write(context.HttpContext, 429, "Límite temporal alcanzado. Intente nuevamente en un minuto.", "rate_limit.exceeded");
            };
        });
    }

    public static void UseCore(WebApplication app, bool isLocal, Action<WebApplication>? additionalSecurity = null)
    {
        app.UseExceptionHandler(new ExceptionHandlerOptions { SuppressDiagnosticsCallback = _ => true });
        app.Use(async (context, next) =>
        {
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            context.Response.Headers.ContentSecurityPolicy = BrowserSecurityPolicy.Default;
            if (isLocal)
            {
                context.Response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
                context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
                context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
            }
            if (context.Request.Path.StartsWithSegments("/api")) context.Response.Headers.CacheControl = "no-store";
            await next(context);
        });
        app.UseStatusCodePages(context => ApiProblems.Write(context.HttpContext,
            context.HttpContext.Response.StatusCode, "No se pudo completar la solicitud.",
            $"http.{context.HttpContext.Response.StatusCode}"));
        if (isLocal)
        {
            // HTML must always pass through the nonce renderer, including alternate URL spellings.
            var contentTypes = new FileExtensionContentTypeProvider();
            foreach (var extension in contentTypes.Mappings.Where(entry => entry.Value == "text/html")
                .Select(entry => entry.Key).ToArray())
                contentTypes.Mappings.Remove(extension);
            app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = contentTypes });
        }
        app.UseRouting();
        if (!isLocal)
        {
            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseCors("Frontend");
        }
        app.UseAuthentication();
        app.UseRateLimiter();
        additionalSecurity?.Invoke(app);
        app.UseAuthorization();
        app.UseMiddleware<RequestBodyLimitMiddleware>();
        app.MapControllers();
        app.MapGet("/health/live", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
        app.MapGet("/api/v1/version", () => Results.Ok(new
        {
            version = app.Configuration["Release:Version"] ?? typeof(ApiServices).Assembly.GetName().Version?.ToString(),
            profile = isLocal ? "local" : "web",
            commit = app.Configuration["Release:Commit"] ?? "development"
        })).AllowAnonymous();
        if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("OpenApi:Enabled"))
            app.MapOpenApi().RequireAuthorization(Permissions.Evaluate);
    }
}
