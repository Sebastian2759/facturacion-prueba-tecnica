using Api.Authentication;
using Api.Bootstrap;
using Api.Handlers;
using Api.Session;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsEnvironment("LocalEvaluation") ||
    builder.Configuration.GetValue<bool>("LocalEvaluation:Enabled") ||
    !string.IsNullOrEmpty(builder.Configuration["Local:InstallationId"]))
    throw new InvalidOperationException("El artifact web no admite LocalEvaluation. Use el paquete local separado.");

var authority = builder.Configuration["Identity:Authority"];
var audience = builder.Configuration["Identity:Audience"];
var clientId = builder.Configuration["Identity:ClientId"];
if (!Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri) ||
    authorityUri.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(audience) ||
    string.IsNullOrWhiteSpace(clientId) || audience == clientId ||
    string.IsNullOrWhiteSpace(builder.Configuration["Identity:Scope"]))
    throw new InvalidOperationException("Configure Authority HTTPS, Audience de API, ClientId SPA distinto y Scope.");

ApiServices.AddCore(builder, isLocal: false);
builder.Services.AddScoped<IHostSession, WebSession>();
var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
if (origins.Length == 0 || origins.Any(origin =>
    !Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
    uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) ||
    !string.IsNullOrEmpty(uri.Fragment) || origin.Contains('*')))
    throw new InvalidOperationException("AllowedOrigins requiere los orígenes HTTPS exactos del frontend.");

builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(origins).WithMethods("GET", "POST", "OPTIONS")
        .WithHeaders("Content-Type", "Authorization", "Idempotency-Key")));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.Audience = audience;
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidAudience = audience,
            ValidIssuer = builder.Configuration["Identity:Issuer"],
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256, SecurityAlgorithms.EcdsaSha256],
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
            RoleClaimType = "roles"
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.Headers.WWWAuthenticate = "Bearer";
                await ApiProblems.Write(context.HttpContext, 401, "Inicie sesión para continuar.", "authentication.required");
            },
            OnForbidden = context => ApiProblems.Write(context.HttpContext, 403,
                "Su sesión no tiene permiso para esta operación.", "access.denied")
        };
    })
    .AddScheme<ApiKeyOptions, ApiKeyAuthHandler>(ApiKeyDefaults.SchemeName, options =>
    {
        options.HeaderName = "X-Api-Key";
        options.Key = builder.Configuration["ApiKey:Key"] ?? string.Empty;
    });

var app = builder.Build();
ApiServices.UseCore(app, isLocal: false);
app.Run();

public partial class Program { }
