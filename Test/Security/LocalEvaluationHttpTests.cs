using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace Test.Security;

public sealed class LocalEvaluationHttpTests
{
    [Fact]
    public async Task LegacySimulatedWrite_IsRejectedBeforeCallingProvider()
    {
        using var factory = new LocalEvaluationFactory(enableRealAccounts: true);
        using var client = factory.Client();
        var session = await LocalEvaluationFactory.SessionAsync(client);
        using var request = LocalEvaluationFactory.Preview(session, csrfToken: session.CsrfToken,
            json: """{"mode":"simulated","code":"LEGACY"}""");
        request.RequestUri = new Uri("/api/v1/accounts", UriKind.Relative);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("/api/v1/inexistente")]
    [InlineData("/api/v1/inexistente.json")]
    [InlineData("/api/v1/demo/invoices")]
    [InlineData("/api/v1/demo-architectures")]
    public async Task UnknownApiRoute_ReturnsProblemInsteadOfAngularHtml(string path)
    {
        using var factory = new LocalEvaluationFactory();
        using var client = factory.Client();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        using var problem = await JsonAsync(response);
        problem.RootElement.GetProperty("code").GetString().Should().Be("resource.not_found");
    }

    [Fact]
    public async Task PublicStaticAssets_AreAccessibleBeforeLocalSession()
    {
        using var factory = new LocalEvaluationFactory();
        using var client = factory.Client();

        using var index = await client.GetAsync("/", TestContext.Current.CancellationToken);
        using var asset = await client.GetAsync("/main.js", TestContext.Current.CancellationToken);
        using var health = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        index.StatusCode.Should().Be(HttpStatusCode.OK);
        asset.StatusCode.Should().Be(HttpStatusCode.OK, "Angular must load before it can establish the local session");
        (await asset.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Contain("local-security-static-fixture");
        asset.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        asset.Headers.GetValues("Content-Security-Policy").Should().ContainSingle()
            .Which.Should().Contain("frame-ancestors 'none'");
        health.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("attacker.example")]
    [InlineData("127.0.0.1:5099")]
    public async Task UnexpectedHost_IsRejected(string host)
    {
        using var factory = new LocalEvaluationFactory();
        using var client = factory.Client();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/session");
        request.Headers.Host = host;

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("X-Forwarded-Host", "127.0.0.1:5080")]
    [InlineData("Forwarded", "host=127.0.0.1:5080")]
    public async Task ForwardedHostHeaders_AreRejectedEvenWhenHostMatches(string header, string value)
    {
        using var factory = new LocalEvaluationFactory();
        using var client = factory.Client();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/session");
        request.Headers.TryAddWithoutValidation(header, value);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AnonymousWrite_RequiresSessionBeforeAcceptingOrigin()
    {
        using var factory = new LocalEvaluationFactory();
        using var client = factory.Client();
        using var request = LocalEvaluationFactory.Preview(null);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var problem = await JsonAsync(response);
        problem.RootElement.GetProperty("code").GetString().Should().Be("authentication.required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("null")]
    [InlineData("https://attacker.example")]
    [InlineData("http://127.0.0.1:5099")]
    public async Task WriteWithoutExactOrigin_IsForbidden(string? origin)
    {
        using var factory = new LocalEvaluationFactory();
        using var client = factory.Client();
        var session = await LocalEvaluationFactory.SessionAsync(client);
        using var request = LocalEvaluationFactory.Preview(session, origin, session.CsrfToken);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var problem = await JsonAsync(response);
        problem.RootElement.GetProperty("code").GetString().Should().Be("local.origin_invalid");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid-antiforgery-token")]
    public async Task WriteWithoutValidAntiforgeryToken_IsRejected(string? token)
    {
        using var factory = new LocalEvaluationFactory();
        using var client = factory.Client();
        var session = await LocalEvaluationFactory.SessionAsync(client);
        using var request = LocalEvaluationFactory.Preview(session, csrfToken: token);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var problem = await JsonAsync(response);
        problem.RootElement.GetProperty("code").GetString().Should().Be("csrf.invalid");
    }

    [Fact]
    public async Task SessionAndAntiforgery_AllowPreviewWithoutSqlOrCloud()
    {
        using var factory = new LocalEvaluationFactory();
        using var client = factory.Client();
        var session = await LocalEvaluationFactory.SessionAsync(client);
        session.CanEvaluate.Should().BeTrue();
        session.CanUseRealAccounts.Should().BeFalse();
        session.CsrfToken.Should().NotBeNullOrWhiteSpace();
        using var request = LocalEvaluationFactory.Preview(session, csrfToken: session.CsrfToken);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var json = await JsonAsync(response);
        json.RootElement.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
        json.RootElement.GetProperty("data").GetProperty("gross").GetDecimal().Should().Be(500_000m);
        json.RootElement.GetProperty("data").GetProperty("total").GetDecimal().Should().Be(565_250m);
        json.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task DefaultLocalSession_CannotEnableRealAccounts()
    {
        using var factory = new LocalEvaluationFactory();
        using var client = factory.Client();
        var session = await LocalEvaluationFactory.SessionAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/accounts");
        request.Headers.TryAddWithoutValidation("Cookie", session.Cookie);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var problem = await JsonAsync(response);
        problem.RootElement.GetProperty("code").GetString().Should().Be("access.denied");
    }

    [Fact]
    public async Task ServerConfiguredLocalSession_AdvertisesRealAccountsWithoutDisclosingCredentials()
    {
        using var factory = new LocalEvaluationFactory(enableRealAccounts: true);
        using var client = factory.Client();
        using var response = await client.GetAsync("/api/v1/session", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(text);
        json.RootElement.GetProperty("data").GetProperty("canUseRealAccounts").GetBoolean().Should().BeTrue();
        text.Should().NotContain("fake-test-password").And.NotContain("test-user").And.NotContain("test-connection");
    }

    [Fact]
    public async Task CookieFromAnotherInstallation_DoesNotAuthorizeWrites()
    {
        using var firstFactory = new LocalEvaluationFactory("ddbe6210-a758-4da5-b2b6-98c63e3a5a01");
        using var firstClient = firstFactory.Client();
        var firstSession = await LocalEvaluationFactory.SessionAsync(firstClient);
        using var secondFactory = new LocalEvaluationFactory("c0cdf2c1-aae0-4ba5-8cc8-33d5481d7102");
        using var secondClient = secondFactory.Client();
        using var request = LocalEvaluationFactory.Preview(firstSession, csrfToken: firstSession.CsrfToken);

        using var response = await secondClient.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidatorErrors_ExposeJsonFieldPathsAndTraceWithoutInternalDetails()
    {
        using var factory = new LocalEvaluationFactory();
        using var client = factory.Client();
        var session = await LocalEvaluationFactory.SessionAsync(client);
        using var request = LocalEvaluationFactory.Preview(session, csrfToken: session.CsrfToken,
            json: """{"details":[{"code":1,"quantity":0,"unitPrice":20}]}""");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var problem = await JsonAsync(response);
        problem.RootElement.GetProperty("code").GetString().Should().Be("validation.failed");
        problem.RootElement.GetProperty("fieldErrors").GetProperty("details[0].quantity").GetArrayLength()
            .Should().BeGreaterThan(0);
        problem.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
        problem.RootElement.ToString().Should().NotContain("StackTrace").And.NotContain("SqlException");
    }

    [Fact]
    public async Task JsonBindingError_DoesNotEchoSubmittedSensitiveMarker()
    {
        using var factory = new LocalEvaluationFactory();
        using var client = factory.Client();
        var session = await LocalEvaluationFactory.SessionAsync(client);
        using var request = LocalEvaluationFactory.Preview(session, csrfToken: session.CsrfToken,
            json: """{"details":[{"code":1,"quantity":"PRIVATE-TEST-MARKER","unitPrice":20}]}""");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().NotContain("PRIVATE-TEST-MARKER");
        using var problem = JsonDocument.Parse(body);
        problem.RootElement.GetProperty("code").GetString().Should().Be("validation.failed");
        problem.RootElement.GetProperty("fieldErrors").GetProperty("details[0].quantity").GetArrayLength()
            .Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BodyAboveLimit_IsRejectedWithOrWithoutContentLength(bool unknownLength)
    {
        using var factory = new LocalEvaluationFactory();
        using var client = factory.Client();
        var session = await LocalEvaluationFactory.SessionAsync(client);
        var json = "{\"details\":[],\"padding\":\"" + new string('x', 256 * 1024) + "\"}";
        using var request = LocalEvaluationFactory.Preview(session, csrfToken: session.CsrfToken, json: json);
        if (unknownLength)
        {
            request.Content!.Dispose();
            request.Content = UnknownLengthJsonContent.Create(json);
        }

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        using var problem = await JsonAsync(response);
        problem.RootElement.GetProperty("status").GetInt32().Should().Be(413);
        problem.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private static async Task<JsonDocument> JsonAsync(HttpResponseMessage response)
        => JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
}
