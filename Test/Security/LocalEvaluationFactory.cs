using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Test.Security;

internal sealed class LocalEvaluationFactory(string? installationId = null, bool enableOpenApi = false)
    : WebApplicationFactory<LocalEvaluation.LocalProgram>
{
    public const string Origin = "http://127.0.0.1:5080";
    private readonly string _installationId = installationId ?? "fd04b061-6eb2-4d5d-9af2-d335a9904308";
    private readonly string _webRoot = CreateWebRoot();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Local:Origin", Origin);
        builder.UseSetting("Local:InstallationId", _installationId);
        builder.UseSetting("ConnectionStrings:AppDbContext",
            "Server=127.0.0.1,1;Database=NoSqlUsedByHttpSecurityTests;Integrated Security=true;Encrypt=true;Connect Timeout=1;");
        builder.UseSetting("Novasoft:Enabled", "false");
        builder.UseSetting("OpenApi:Enabled", enableOpenApi ? "true" : "false");
        builder.UseWebRoot(_webRoot);
    }

    public HttpClient Client()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(Origin),
            AllowAutoRedirect = false,
            HandleCookies = false,
        });
        client.Timeout = TimeSpan.FromSeconds(15);
        return client;
    }

    public static async Task<LocalTestSession> SessionAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/session", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var data = json.RootElement.GetProperty("data");
        var cookies = string.Join("; ", response.Headers.GetValues("Set-Cookie")
            .Select(value => value.Split(';', 2)[0]));
        return new LocalTestSession(cookies, data.GetProperty("csrfToken").GetString()!,
            data.GetProperty("canEvaluate").GetBoolean(), data.GetProperty("canUseRealAccounts").GetBoolean());
    }

    public static HttpRequestMessage Preview(LocalTestSession? session,
        string? origin = Origin, string? csrfToken = null, string? json = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/invoices/preview")
        {
            Content = new StringContent(json ??
                """{"details":[{"code":10,"quantity":2,"unitPrice":250000}]}""", Encoding.UTF8, "application/json"),
        };
        if (session is not null)
            request.Headers.TryAddWithoutValidation("Cookie", session.Cookie);
        if (origin is not null)
            request.Headers.TryAddWithoutValidation("Origin", origin);
        if (csrfToken is not null)
            request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", csrfToken);
        return request;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_webRoot))
        {
            var expectedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "facturacion-security-test-assets"));
            var actual = Path.GetFullPath(_webRoot);
            if (!actual.StartsWith(expectedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Refusing to remove a directory outside the test asset root.");
            Directory.Delete(actual, recursive: true);
        }
    }

    private static string CreateWebRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "facturacion-security-test-assets", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "index.html"), "<!doctype html><html><body>Local security fixture</body></html>");
        File.WriteAllText(Path.Combine(path, "main.js"), "/* local-security-static-fixture */");
        return path;
    }
}

internal sealed record LocalTestSession(string Cookie, string CsrfToken, bool CanEvaluate, bool CanUseRealAccounts);

internal sealed class UnknownLengthJsonContent(byte[] bytes) : HttpContent
{
    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => stream.WriteAsync(bytes).AsTask();

    public static UnknownLengthJsonContent Create(string json)
    {
        var content = new UnknownLengthJsonContent(Encoding.UTF8.GetBytes(json));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }
}
