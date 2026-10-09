using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Contracts.Accounts;

namespace Adapters.Accounts;

/// <summary>Only talks to the fixed HTTPS test provider. Transport has no retries or redirect following.</summary>
public sealed class NovasoftClient(HttpClient httpClient, NovasoftOptions options, NovasoftTokenCache tokenCache)
{
    public static readonly Uri BaseAddress = new("https://test.novasoft.com.co:8091/webapi/");
    private const string AccountsPath = "api/CXC/Senior/Accounts";
    private const int MaximumResponseBytes = 2 * 1024 * 1024;
    private const int MaximumAccountCount = 5000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<AccountRecord>> GetAsync(string? code, CancellationToken cancellationToken)
    {
        var path = code is null ? AccountsPath : $"{AccountsPath}/{Uri.EscapeDataString(code)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(BaseAddress, path));
        using var response = await SendAuthenticatedAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound && code is not null)
            return [];

        EnsureSuccess(response, false);
        var accounts = await ReadJsonAsync<List<NovasoftAccount>>(response, cancellationToken);
        if (accounts.Count > MaximumAccountCount || accounts.Any(account => account is null || string.IsNullOrWhiteSpace(account.Code)))
            throw InvalidResponse();

        return accounts.Select(account => account.ToRecord()).ToArray();
    }

    public async Task CreateAsync(AccountRecord account, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(BaseAddress, AccountsPath))
        {
            Content = JsonContent.Create(NovasoftAccount.FromRecord(account), options: JsonOptions),
        };
        using var response = await SendAuthenticatedAsync(request, cancellationToken);
        EnsureSuccess(response, true);
        var result = await ReadJsonAsync<NovasoftResult>(response, cancellationToken);
        if (result.Status is >= 400 || result.Errors is { Count: > 0 })
            throw new AccountsIntegrationException("PROVIDER_REJECTED", 422, "El proveedor no aceptó la cuenta. Revise los campos y los catálogos de Novasoft.");
        if (result.Status is not (>= 200 and <= 299))
            throw InvalidResponse();
    }

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!options.IsConfigured)
            throw new AccountsIntegrationException("PROVIDER_NOT_CONFIGURED", 503, "La integración real no está configurada.");

        var token = await tokenCache.GetAsync(LoginAsync, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            response.Dispose();
            await tokenCache.InvalidateAsync(token, cancellationToken);
            throw new AccountsIntegrationException("PROVIDER_AUTHENTICATION_FAILED", 502, "El proveedor rechazó la sesión de integración.");
        }

        return response;
    }

    private async Task<(string Token, DateTimeOffset Expiration)> LoginAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(BaseAddress, "api/Cuenta/Login"))
        {
            Content = JsonContent.Create(new
            {
                userLogin = options.UserLogin,
                password = options.Password,
                connectionName = options.ConnectionName,
            }),
        };
        using var response = await SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new AccountsIntegrationException("PROVIDER_AUTHENTICATION_FAILED", 502, "No fue posible iniciar la sesión de integración.");

        var result = await ReadJsonAsync<NovasoftLoginResult>(response, cancellationToken);
        if (string.IsNullOrWhiteSpace(result.Token) || result.Token.Length > 16_384 ||
            result.Token.Any(character => !char.IsAsciiLetterOrDigit(character) && !"-._~+/=".Contains(character)) ||
            !DateTimeOffset.TryParse(result.Expiration, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var expiration))
            throw InvalidResponse();

        return (result.Token, expiration);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        try
        {
            // ResponseContentRead applies HttpClient.Timeout to the bounded body, not just headers.
            return await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            var message = request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath.EndsWith("/Accounts", StringComparison.Ordinal) == true
                ? "La respuesta del proveedor tardó demasiado; el resultado de la creación es incierto. Consulte el código antes de volver a enviarla."
                : "El proveedor tardó demasiado en responder.";
            throw new AccountsIntegrationException("PROVIDER_TIMEOUT", 504, message);
        }
        catch (HttpRequestException)
        {
            throw new AccountsIntegrationException("PROVIDER_UNAVAILABLE", 502, "No fue posible completar la comunicación con el proveedor. Si intentaba crear una cuenta, consulte su código antes de volver a enviarla.");
        }
    }

    private static void EnsureSuccess(HttpResponseMessage response, bool isCreate)
    {
        if (response.IsSuccessStatusCode)
            return;
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new AccountsIntegrationException("PROVIDER_CONFLICT", 409, "El proveedor informa un conflicto con la cuenta.");
        if (isCreate && response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
            throw new AccountsIntegrationException("PROVIDER_REJECTED", 422, "El proveedor no aceptó la cuenta. Revise los campos y los catálogos de Novasoft.");
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new AccountsIntegrationException("PROVIDER_BUSY", 503, "El proveedor está limitando las solicitudes. Intente consultar más tarde.");

        throw new AccountsIntegrationException("PROVIDER_UNAVAILABLE", 502, "El proveedor no pudo completar la operación.");
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength > MaximumResponseBytes)
            throw InvalidResponse();
        try
        {
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (bytes.Length > MaximumResponseBytes)
                throw InvalidResponse();
            return JsonSerializer.Deserialize<T>(bytes, JsonOptions) ?? throw InvalidResponse();
        }
        catch (JsonException)
        {
            throw InvalidResponse();
        }
    }

    private static AccountsIntegrationException InvalidResponse() =>
        new("PROVIDER_INVALID_RESPONSE", 502, "El proveedor entregó una respuesta que no se puede validar. Si estaba creando una cuenta, consulte su código antes de volver a enviarla.");

    private sealed class NovasoftLoginResult
    {
        public string? Token { get; init; }
        public string? Expiration { get; init; }
    }

    private sealed class NovasoftResult
    {
        public int? Status { get; init; }
        public List<JsonElement>? Errors { get; init; }
    }

    private sealed class NovasoftAccount
    {
        [JsonPropertyName("codCli")] public string? Code { get; init; }
        [JsonPropertyName("tipIde")] public string? IdentificationType { get; init; }
        [JsonPropertyName("nitCli")] public string? Document { get; init; }
        [JsonPropertyName("nomCli")] public string? Name { get; init; }
        [JsonPropertyName("nom1Cli")] public string? FirstName { get; init; }
        [JsonPropertyName("ap1Cli")] public string? LastName { get; init; }
        [JsonPropertyName("eMail")] public string? Email { get; init; }
        [JsonPropertyName("tipPer")] public int PersonType { get; init; }
        [JsonPropertyName("codCiu")] public string? CityCode { get; init; }
        [JsonPropertyName("codDep")] public string? DepartmentCode { get; init; }
        [JsonPropertyName("codPai")] public string? CountryCode { get; init; }
        [JsonPropertyName("di1Cli")] public string? Address { get; init; }
        [JsonPropertyName("di2Cli")] public string? AdditionalAddress { get; init; }
        [JsonPropertyName("te1Cli")] public string? Phone { get; init; }
        [JsonPropertyName("tipCli")] public int CustomerType { get; init; }
        [JsonPropertyName("pagWeb")] public string? Website { get; init; }
        [JsonPropertyName("fecIng")] public DateTimeOffset RegisteredAt { get; init; }
        [JsonPropertyName("codCliExtr")] public string? ExternalCode { get; init; }

        public AccountRecord ToRecord() => new(
            Code!, IdentificationType ?? string.Empty, Document ?? string.Empty, Name ?? string.Empty,
            FirstName, LastName, Email ?? string.Empty, PersonType, CityCode ?? string.Empty,
            DepartmentCode ?? string.Empty, CountryCode ?? string.Empty, Address, AdditionalAddress,
            Phone, CustomerType, Website, RegisteredAt, ExternalCode);

        public static NovasoftAccount FromRecord(AccountRecord account) => new()
        {
            Code = account.Code,
            IdentificationType = account.IdentificationType,
            Document = account.Document,
            Name = account.Name,
            FirstName = account.FirstName,
            LastName = account.LastName,
            Email = account.Email,
            PersonType = account.PersonType,
            CityCode = account.CityCode,
            DepartmentCode = account.DepartmentCode,
            CountryCode = account.CountryCode,
            Address = account.Address,
            AdditionalAddress = account.AdditionalAddress,
            Phone = account.Phone,
            CustomerType = account.CustomerType,
            Website = account.Website,
            RegisteredAt = account.RegisteredAt,
            ExternalCode = account.ExternalCode,
        };
    }
}
