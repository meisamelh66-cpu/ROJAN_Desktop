using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Rojan.Server.Domain.Authentication;
using Rojan.Server.Infrastructure.Security;

namespace Rojan.Server.Infrastructure.Tests.Security;

/// <summary>Exercises <see cref="ClaimsTenantContext"/> - the "Infrastructure: JWT claims -> TenantContext" requirement this commit's own task list calls out. Uses a real <see cref="JwtTokenService"/> round-trip (generate, then validate) so the <see cref="System.Security.Claims.ClaimsPrincipal"/> under test is exactly what the real JWT bearer pipeline would produce, not a hand-built stand-in.</summary>
public sealed class ClaimsTenantContextTests
{
    private static readonly JwtTokenService TokenService = new(Options.Create(new JwtOptions
    {
        Issuer = "RojanServer.Tests",
        Audience = "RojanClients.Tests",
        SigningKey = "test-only-signing-key-not-used-anywhere-real-1234567890",
        AccessTokenLifetimeMinutes = 60,
    }));

    private static ClaimsTenantContext CreateSut(User user)
    {
        var token = TokenService.GenerateAccessToken(user);
        var principal = TokenService.ValidateAccessToken(token)!;
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } };

        return new ClaimsTenantContext(accessor);
    }

    private static User MakeUser(string? branchId) =>
        new("user-1", "org-1", branchId, "owner@rojan.example", "hash", "Noah Bennett", UserRoles.Owner, DateTimeOffset.UtcNow);

    [Fact]
    public void OrganizationId_AuthenticatedRequest_ReturnsOrgIdClaim()
    {
        var sut = CreateSut(MakeUser("branch-1"));

        Assert.Equal("org-1", sut.OrganizationId);
    }

    [Fact]
    public void UserId_AuthenticatedRequest_ReturnsSubClaim()
    {
        var sut = CreateSut(MakeUser("branch-1"));

        Assert.Equal("user-1", sut.UserId);
    }

    [Fact]
    public void BranchId_UserHasABranch_ReturnsBranchIdClaim()
    {
        var sut = CreateSut(MakeUser("branch-1"));

        Assert.Equal("branch-1", sut.BranchId);
    }

    [Fact]
    public void BranchId_UserHasNoBranch_ReturnsNull()
    {
        var sut = CreateSut(MakeUser(branchId: null));

        Assert.Null(sut.BranchId);
    }

    [Fact]
    public void OrganizationId_NoHttpContext_ThrowsInvalidOperationException()
    {
        var sut = new ClaimsTenantContext(new HttpContextAccessor());

        Assert.Throws<InvalidOperationException>(() => sut.OrganizationId);
    }

    [Fact]
    public void OrganizationId_UnauthenticatedHttpContext_ThrowsInvalidOperationException()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var sut = new ClaimsTenantContext(accessor);

        Assert.Throws<InvalidOperationException>(() => sut.OrganizationId);
    }
}
