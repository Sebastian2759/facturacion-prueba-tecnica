using System.Security.Claims;
using Api.Security;
using Domain.Base;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Test.Security;

public sealed class HttpCurrentUserTests
{
    [Fact]
    public void Subject_IsStableOpaqueAndSeparatesIssuerAndSubject()
    {
        var first = User("a", "bc").Subject;
        var same = User("a", "bc").Subject;
        var otherIssuer = User("ab", "c").Subject;
        var otherSubject = User("a", "bd").Subject;

        first.Should().HaveLength(64);
        first.Should().Be(same);
        first.Should().NotBe(otherIssuer);
        first.Should().NotBe(otherSubject);
    }

    [Theory]
    [InlineData(null, "subject")]
    [InlineData("issuer", null)]
    [InlineData("", "subject")]
    public void Subject_RequiresAuthenticatedIssuerAndSubject(string? issuer, string? subject)
    {
        var user = User(issuer, subject);
        Action getSubject = () => _ = user.Subject;

        getSubject.Should().Throw<AccessDeniedException>();
    }

    [Fact]
    public void AnonymousPrincipal_CannotGrantItselfEvaluationByClaim()
    {
        var identity = new ClaimsIdentity([new Claim("roles", Permissions.Evaluate)]);
        var user = new HttpCurrentUser(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        });

        user.CanEvaluate.Should().BeFalse();
        Action getSubject = () => _ = user.Subject;
        getSubject.Should().Throw<AccessDeniedException>();
    }

    [Fact]
    public void PermissionForEvaluation_DoesNotGrantRealAccounts()
    {
        var user = User("issuer", "subject", Permissions.Evaluate);

        user.CanEvaluate.Should().BeTrue();
        user.CanUseRealAccounts.Should().BeFalse();
    }

    [Fact]
    public void Permissions_RequireExactValuesAndRealPermissionIsExplicit()
    {
        User("issuer", "subject", "INVOICES.EVALUATE").CanEvaluate.Should().BeFalse();
        User("issuer", "subject", Permissions.RealAccounts).CanUseRealAccounts.Should().BeTrue();
        User("issuer", "subject").CanUseRealAccounts.Should().BeFalse();
    }

    private static HttpCurrentUser User(string? issuer, string? subject, string? role = null)
    {
        var claims = new List<Claim>();
        if (issuer is not null) claims.Add(new Claim("iss", issuer));
        if (subject is not null) claims.Add(new Claim("sub", subject));
        if (role is not null) claims.Add(new Claim("roles", role));
        var identity = new ClaimsIdentity(claims, "test-validated-principal");
        return new HttpCurrentUser(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        });
    }
}
