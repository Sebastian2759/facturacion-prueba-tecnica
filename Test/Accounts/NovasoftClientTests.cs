using System.Net;
using System.Text;
using System.Text.Json;
using Adapters.Accounts;
using Domain.Contracts.Accounts;

namespace Test.Accounts;

public sealed class NovasoftClientTests
{
    [Fact]
    public async Task GetAsync_LogsInOnServerAndSendsBearerToAccountsOnly()
    {
        using var harness = new ClientHarness((request, index) =>
        {
            Assert.Equal("https", request.RequestUri!.Scheme);
            Assert.Equal("test.novasoft.com.co", request.RequestUri.Host);
            if (index == 1)
            {
                Assert.Equal("/webapi/api/Cuenta/Login", request.RequestUri.AbsolutePath);
                Assert.Null(request.Headers.Authorization);
                return ClientHarness.Login();
            }

            Assert.Equal("/webapi/api/CXC/Senior/Accounts/TEST001", request.RequestUri.AbsolutePath);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            Assert.Equal("fake-provider-token", request.Headers.Authorization.Parameter);
            return ClientHarness.Json("""[{"codCli":"TEST001","nomCli":"Ejemplo","nitCli":"900000010","fecIng":"2026-10-09T14:00:00Z"}]""");
        });

        var accounts = await harness.Client.GetAsync("TEST001", CancellationToken.None);

        Assert.Equal("TEST001", Assert.Single(accounts).Code);
        using var login = JsonDocument.Parse(harness.Handler.Bodies[0]!);
        Assert.Equal("test-user", login.RootElement.GetProperty("userLogin").GetString());
        Assert.Equal("fake-test-password", login.RootElement.GetProperty("password").GetString());
        Assert.Equal("test-connection", login.RootElement.GetProperty("connectionName").GetString());
        Assert.Equal(2, harness.Handler.Count);
    }

    [Fact]
    public async Task CreateAsync_MapsNormalizedFieldsAndAcceptsProviderAcknowledgement()
    {
        using var harness = new ClientHarness((_, index) => index == 1
            ? ClientHarness.Login()
            : ClientHarness.Json("""{"status":200,"errors":null}"""));

        await harness.Client.CreateAsync(AccountTestData.Record(), CancellationToken.None);

        using var payload = JsonDocument.Parse(harness.Handler.Bodies[1]!);
        Assert.Equal("TEST001", payload.RootElement.GetProperty("codCli").GetString());
        Assert.Equal("CC", payload.RootElement.GetProperty("tipIde").GetString());
        Assert.Equal("900000010", payload.RootElement.GetProperty("nitCli").GetString());
        Assert.Equal("Persona Ficticia", payload.RootElement.GetProperty("nomCli").GetString());
        Assert.Equal("test@example.invalid", payload.RootElement.GetProperty("eMail").GetString());
        Assert.Equal("11001", payload.RootElement.GetProperty("codCiu").GetString());
        Assert.Equal(AccountTestData.Now, payload.RootElement.GetProperty("fecIng").GetDateTimeOffset());
        Assert.False(payload.RootElement.TryGetProperty("estCli", out _));
        Assert.False(payload.RootElement.TryGetProperty("mode", out _));
        Assert.Equal(2, harness.Handler.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "PROVIDER_REJECTED", 422)]
    [InlineData(HttpStatusCode.Conflict, "PROVIDER_CONFLICT", 409)]
    [InlineData(HttpStatusCode.Unauthorized, "PROVIDER_AUTHENTICATION_FAILED", 502)]
    [InlineData(HttpStatusCode.Forbidden, "PROVIDER_AUTHENTICATION_FAILED", 502)]
    [InlineData(HttpStatusCode.InternalServerError, "PROVIDER_UNAVAILABLE", 502)]
    [InlineData(HttpStatusCode.Redirect, "PROVIDER_UNAVAILABLE", 502)]
    public async Task CreateAsync_NormalizesErrorsWithoutRawBodyOrAutomaticRetry(HttpStatusCode status, string code, int httpStatus)
    {
        using var harness = new ClientHarness((_, index) => index == 1
            ? ClientHarness.Login()
            : ClientHarness.Json("{\"detail\":\"DO-NOT-LEAK-RAW-SECRET\"}", status));

        var error = await Assert.ThrowsAsync<AccountsIntegrationException>(() => harness.Client.CreateAsync(AccountTestData.Record(), CancellationToken.None));

        Assert.Equal(code, error.Code);
        Assert.Equal(httpStatus, error.HttpStatus);
        Assert.DoesNotContain("DO-NOT-LEAK", error.ToString());
        Assert.Null(error.InnerException);
        Assert.Equal(2, harness.Handler.Count);
    }

    [Theory]
    [InlineData("{\"status\":400,\"errors\":[]}", "PROVIDER_REJECTED")]
    [InlineData("{\"status\":200,\"errors\":[{\"detail\":\"hidden\"}]}", "PROVIDER_REJECTED")]
    [InlineData("{}", "PROVIDER_INVALID_RESPONSE")]
    [InlineData("not-json", "PROVIDER_INVALID_RESPONSE")]
    [InlineData("null", "PROVIDER_INVALID_RESPONSE")]
    public async Task CreateAsync_DoesNotAssumeHttp200MeansBusinessSuccess(string payload, string code)
    {
        using var harness = new ClientHarness((_, index) => index == 1 ? ClientHarness.Login() : ClientHarness.Json(payload));

        var error = await Assert.ThrowsAsync<AccountsIntegrationException>(() => harness.Client.CreateAsync(AccountTestData.Record(), CancellationToken.None));

        Assert.Equal(code, error.Code);
        Assert.Equal(2, harness.Handler.Count);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("[{\"nomCli\":\"missing code\"}]")]
    public async Task GetAsync_RejectsUnexpectedShape(string payload)
    {
        using var harness = new ClientHarness((_, index) => index == 1 ? ClientHarness.Login() : ClientHarness.Json(payload));

        var error = await Assert.ThrowsAsync<AccountsIntegrationException>(() => harness.Client.GetAsync(null, CancellationToken.None));

        Assert.Equal("PROVIDER_INVALID_RESPONSE", error.Code);
    }

    [Fact]
    public async Task GetAsync_HandlesNotFoundCodeAsEmptyArray()
    {
        using var harness = new ClientHarness((_, index) => index == 1 ? ClientHarness.Login() : ClientHarness.Json("{}", HttpStatusCode.NotFound));

        Assert.Empty(await harness.Client.GetAsync("MISSING", CancellationToken.None));
    }

    [Fact]
    public async Task RejectedSessionIsInvalidatedForNextOperationWithoutRetryingCurrentWrite()
    {
        using var harness = new ClientHarness((request, index) => request.RequestUri!.AbsolutePath.EndsWith("/Login", StringComparison.Ordinal)
            ? ClientHarness.Login()
            : index == 2 ? ClientHarness.Json("{}", HttpStatusCode.Unauthorized) : ClientHarness.Json("[]"));

        await Assert.ThrowsAsync<AccountsIntegrationException>(() => harness.Client.CreateAsync(AccountTestData.Record(), CancellationToken.None));
        Assert.Equal(2, harness.Handler.Count);
        await harness.Client.GetAsync(null, CancellationToken.None);
        Assert.Equal(4, harness.Handler.Count);
    }

    [Fact]
    public async Task ExpiredLoginTokenNeverReachesAccounts()
    {
        using var harness = new ClientHarness((_, _) => ClientHarness.Json("""{"token":"expired","expiration":"2026-10-09T13:59:00Z"}"""));

        var error = await Assert.ThrowsAsync<AccountsIntegrationException>(() => harness.Client.GetAsync(null, CancellationToken.None));

        Assert.Equal("PROVIDER_INVALID_TOKEN", error.Code);
        Assert.Equal(1, harness.Handler.Count);
    }

    [Fact]
    public async Task TimeoutOnCreateMarksUncertainOutcomeAndNeverRetries()
    {
        using var harness = new ClientHarness((_, index) => index == 1 ? ClientHarness.Login() : throw new TaskCanceledException());

        var error = await Assert.ThrowsAsync<AccountsIntegrationException>(() => harness.Client.CreateAsync(AccountTestData.Record(), CancellationToken.None));

        Assert.Equal("PROVIDER_TIMEOUT", error.Code);
        Assert.Contains("incierto", error.Message);
        Assert.Equal(2, harness.Handler.Count);
    }

    [Fact]
    public async Task CallerCancellationIsPreserved()
    {
        using var harness = new ClientHarness((_, _) => throw new InvalidOperationException("No request expected."));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => harness.Client.GetAsync(null, cancellation.Token));

        Assert.Equal(0, harness.Handler.Count);
    }

    [Fact]
    public async Task LocalModeCannotCallRealProviderUnlessExplicitlyEnabled()
    {
        using var harness = new ClientHarness((_, _) => throw new InvalidOperationException("No request expected."));
        var gateway = new AccountsGateway(new() { IsLocal = true, RealEnabled = false }, harness.Client, new SimulatedAccountsStore(new AccountsClock()));

        var error = await Assert.ThrowsAsync<AccountsIntegrationException>(() => gateway.GetAsync(AccountExecutionMode.Real, "alice", null, CancellationToken.None));

        Assert.Equal("ACCOUNTS_REAL_DISABLED", error.Code);
        Assert.Equal(0, harness.Handler.Count);
    }

    [Fact]
    public async Task ConfiguredLocalModeCallsProviderThroughAuthenticatedClient()
    {
        using var harness = new ClientHarness((_, index) => index == 1 ? ClientHarness.Login() : ClientHarness.Json("[]"));
        var gateway = new AccountsGateway(new() { IsLocal = true, RealEnabled = true }, harness.Client, new SimulatedAccountsStore(new AccountsClock()));
        var result = await gateway.GetAsync(AccountExecutionMode.Real, "alice", null, CancellationToken.None);
        Assert.Empty(result);
        Assert.Equal(2, harness.Handler.Count);
    }

    private sealed class ClientHarness : IDisposable
    {
        private readonly HttpClient httpClient;
        private readonly NovasoftTokenCache cache;

        public ClientHarness(Func<HttpRequestMessage, int, HttpResponseMessage> respond)
        {
            Handler = new StubHandler(respond);
            httpClient = new(Handler) { MaxResponseContentBufferSize = 2 * 1024 * 1024 };
            cache = new(new AccountsClock());
            Client = new(httpClient, new() { UserLogin = "test-user", Password = "fake-test-password", ConnectionName = "test-connection" }, cache);
        }

        public NovasoftClient Client { get; }
        public StubHandler Handler { get; }
        public static HttpResponseMessage Json(string payload, HttpStatusCode status = HttpStatusCode.OK) => new(status)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };

        public static HttpResponseMessage Login() => Json("""{"token":"fake-provider-token","expiration":"2026-10-09T15:00:00Z"}""");
        public void Dispose()
        {
            cache.Dispose();
            httpClient.Dispose();
        }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, int, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int Count { get; private set; }
        public List<string?> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Count++;
            Bodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
            return respond(request, Count);
        }
    }
}
