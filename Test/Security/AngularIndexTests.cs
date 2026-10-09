using System.Net;
using FluentAssertions;

namespace Test.Security;

public sealed class AngularIndexTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/index.html")]
    [InlineData("/INDEX.HTML")]
    [InlineData("/facturas?nonce=untrusted-query-value")]
    public async Task HtmlShell_UsesFreshMatchingNonceAndCannotBeCached(string path)
    {
        using var factory = new LocalEvaluationFactory();
        using var client = factory.Client();
        using var first = await client.GetAsync(path, TestContext.Current.CancellationToken);
        using var second = await client.GetAsync(path, TestContext.Current.CancellationToken);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstPolicy = first.Headers.GetValues("Content-Security-Policy").Single();
        var secondPolicy = second.Headers.GetValues("Content-Security-Policy").Single();
        firstPolicy.Should().Contain("script-src 'self'").And.NotContain("unsafe-inline").And.NotContain("unsafe-eval");
        firstPolicy.Should().NotBe(secondPolicy, "every HTML response must have an unpredictable nonce");
        var nonce = firstPolicy.Split("'nonce-", StringSplitOptions.None)[1].Split('\'')[0];
        Convert.FromBase64String(nonce).Should().HaveCount(32);
        var html = await first.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        html.Should().Contain($"ngCspNonce=\"{nonce}\"").And.NotContain("__CSP_NONCE__").And.NotContain("untrusted-query-value");
        first.Headers.CacheControl!.NoStore.Should().BeTrue();
        first.Headers.GetValues("Cross-Origin-Embedder-Policy").Should().ContainSingle().Which.Should().Be("require-corp");
        first.Headers.GetValues("Cross-Origin-Opener-Policy").Should().ContainSingle().Which.Should().Be("same-origin");
        first.Headers.GetValues("Cross-Origin-Resource-Policy").Should().ContainSingle().Which.Should().Be("same-origin");
    }

    [Fact]
    public async Task HeadShell_ProvidesPolicyWithoutHtmlBody()
    {
        using var factory = new LocalEvaluationFactory();
        using var client = factory.Client();
        using var request = new HttpRequestMessage(HttpMethod.Head, "/index.html");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("'nonce-");
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
    }

    [Theory]
    [InlineData("GET", "//index.html")]
    [InlineData("HEAD", "//index.html")]
    [InlineData("GET", "///index.html")]
    public async Task AlternateHtmlPaths_CannotExposeTheStaticTemplate(string method, string path)
    {
        using var factory = new LocalEvaluationFactory();
        using var client = factory.Client();
        using var request = new HttpRequestMessage(new HttpMethod(method), new Uri(LocalEvaluationFactory.Origin + path));
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().NotContain("__CSP_NONCE__");
        if (response.IsSuccessStatusCode)
        {
            response.Headers.CacheControl!.NoStore.Should().BeTrue();
            response.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("'nonce-");
        }
    }

    [Fact]
    public async Task OptionsSpaRoute_DoesNotRenderHtml()
    {
        using var factory = new LocalEvaluationFactory();
        using var client = factory.Client();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/facturas");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.Content.Headers.ContentType?.MediaType.Should().NotBe("text/html");
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().NotContain("<app-root");
    }
}
