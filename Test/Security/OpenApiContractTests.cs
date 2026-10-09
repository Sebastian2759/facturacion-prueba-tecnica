using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace Test.Security;

public sealed class OpenApiContractTests
{
    [Fact]
    public async Task EnabledContract_RequiresSessionAndDescribesInvoiceAndAccountOperations()
    {
        using var factory = new LocalEvaluationFactory(enableOpenApi: true);
        using var client = factory.Client();
        using var anonymous = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var session = await LocalEvaluationFactory.SessionAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/openapi/v1.json");
        request.Headers.TryAddWithoutValidation("Cookie", session.Cookie);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        root.GetProperty("openapi").GetString().Should().StartWith("3.");
        var paths = root.GetProperty("paths");
        paths.GetProperty("/api/v1/invoices").TryGetProperty("post", out _).Should().BeTrue();
        paths.GetProperty("/api/v1/invoices/preview").TryGetProperty("post", out _).Should().BeTrue();
        paths.GetProperty("/api/v1/accounts").TryGetProperty("get", out _).Should().BeTrue();
        paths.GetProperty("/api/v1/accounts").TryGetProperty("post", out _).Should().BeTrue();
        root.GetProperty("components").GetProperty("schemas").EnumerateObject().Should().NotBeEmpty();

        // CI exports the actual host contract before image scans, without SQL or provider requests.
        var output = Environment.GetEnvironmentVariable("FACTURACION_OPENAPI_OUTPUT");
        if (!string.IsNullOrWhiteSpace(output))
        {
            var absolute = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
            await File.WriteAllTextAsync(absolute, body, TestContext.Current.CancellationToken);
        }
    }
}
