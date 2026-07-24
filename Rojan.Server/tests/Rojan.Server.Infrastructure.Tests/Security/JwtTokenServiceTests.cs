using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rojan.Server.Domain.Authentication;
using Rojan.Server.Infrastructure.Security;

namespace Rojan.Server.Infrastructure.Tests.Security;

/// <summary>Exercises <see cref="JwtTokenService"/> - including the "Security: invalid token rejected" requirement this commit's own task list calls out explicitly.</summary>
public sealed class JwtTokenServiceTests
{
    private static readonly User TestUser = new(
        "user-1", "org-1", "branch-1", "owner@rojan.example", "hash", "Noah Bennett", UserRoles.Owner, DateTimeOffset.UtcNow);

    private static JwtTokenService CreateSut(int accessTokenLifetimeMinutes = 60) =>
        new(Options.Create(new JwtOptions
        {
            Issuer = "RojanServer.Tests",
            Audience = "RojanClients.Tests",
            SigningKey = "test-only-signing-key-not-used-anywhere-real-1234567890",
            AccessTokenLifetimeMinutes = accessTokenLifetimeMinutes,
        }));

    [Fact]
    public void GenerateAccessToken_ThenValidate_RoundTripsWithExpectedClaims()
    {
        var sut = CreateSut();

        var token = sut.GenerateAccessToken(TestUser);
        var principal = sut.ValidateAccessToken(token);

        Assert.NotNull(principal);
        Assert.Equal("user-1", principal!.FindFirst("sub")?.Value);
        Assert.Equal("org-1", principal.FindFirst("org_id")?.Value);
        Assert.Equal("branch-1", principal.FindFirst("branch_id")?.Value);
        Assert.Equal(UserRoles.Owner, principal.FindFirst(ClaimTypes.Role)?.Value);
    }

    [Fact]
    public void GenerateAccessToken_UserWithNoBranch_OmitsBranchClaim()
    {
        var sut = CreateSut();
        var userWithoutBranch = TestUser with { BranchId = null };

        var token = sut.GenerateAccessToken(userWithoutBranch);
        var principal = sut.ValidateAccessToken(token);

        Assert.NotNull(principal);
        Assert.Null(principal!.FindFirst("branch_id"));
    }

    [Fact]
    public void ValidateAccessToken_TamperedToken_ReturnsNull()
    {
        var sut = CreateSut();
        var token = sut.GenerateAccessToken(TestUser);
        var tampered = token[..^4] + "abcd";

        Assert.Null(sut.ValidateAccessToken(tampered));
    }

    [Fact]
    public void ValidateAccessToken_SignedWithADifferentKey_ReturnsNull()
    {
        var issuer = CreateSut();
        var differentKeyValidator = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "RojanServer.Tests",
            Audience = "RojanClients.Tests",
            SigningKey = "a-completely-different-signing-key-0987654321-zzzzzzzzzz",
            AccessTokenLifetimeMinutes = 60,
        }));

        var token = issuer.GenerateAccessToken(TestUser);

        Assert.Null(differentKeyValidator.ValidateAccessToken(token));
    }

    [Fact]
    public void ValidateAccessToken_ExpiredToken_ReturnsNull()
    {
        // GenerateAccessToken always stamps notBefore as "now", so it cannot
        // itself produce an already-expired token (JwtSecurityToken requires
        // expires > notBefore) - build one directly instead, signed with the
        // same key/issuer/audience, with both timestamps in the past.
        const string signingKey = "test-only-signing-key-not-used-anywhere-real-1234567890";
        var sut = CreateSut();
        var now = DateTime.UtcNow;
        var expiredToken = new JwtSecurityToken(
            "RojanServer.Tests",
            "RojanClients.Tests",
            [new Claim("sub", TestUser.Id)],
            notBefore: now.AddHours(-2),
            expires: now.AddHours(-1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256));
        var token = new JwtSecurityTokenHandler().WriteToken(expiredToken);

        Assert.Null(sut.ValidateAccessToken(token));
    }

    [Fact]
    public void ValidateAccessToken_NotAJwtAtAll_ReturnsNullRatherThanThrowing()
    {
        var sut = CreateSut();

        Assert.Null(sut.ValidateAccessToken("this-is-not-a-jwt"));
    }

    [Fact]
    public void GenerateRefreshTokenValue_ProducesDifferentValuesEachCall()
    {
        var sut = CreateSut();

        var first = sut.GenerateRefreshTokenValue();
        var second = sut.GenerateRefreshTokenValue();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void HashRefreshTokenValue_SameInput_ProducesTheSameHashEveryTime()
    {
        var sut = CreateSut();
        var rawToken = sut.GenerateRefreshTokenValue();

        Assert.Equal(sut.HashRefreshTokenValue(rawToken), sut.HashRefreshTokenValue(rawToken));
    }

    [Fact]
    public void HashRefreshTokenValue_NeverEqualsTheRawTokenItself()
    {
        var sut = CreateSut();
        var rawToken = sut.GenerateRefreshTokenValue();

        Assert.NotEqual(rawToken, sut.HashRefreshTokenValue(rawToken));
    }
}
