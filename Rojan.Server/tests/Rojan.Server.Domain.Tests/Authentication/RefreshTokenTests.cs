using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Domain.Tests.Authentication;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsActive_UnrevokedAndUnexpired_ReturnsTrue()
    {
        var token = new RefreshToken("token-1", "user-1", "hash", Now.AddMinutes(-5), Now.AddDays(30));

        Assert.True(token.IsActive(Now));
        Assert.False(token.IsExpired(Now));
        Assert.False(token.IsRevoked);
    }

    [Fact]
    public void IsActive_Expired_ReturnsFalse()
    {
        var token = new RefreshToken("token-1", "user-1", "hash", Now.AddDays(-31), Now.AddDays(-1));

        Assert.False(token.IsActive(Now));
        Assert.True(token.IsExpired(Now));
    }

    [Fact]
    public void IsActive_Revoked_ReturnsFalseEvenWhenNotExpired()
    {
        var token = new RefreshToken("token-1", "user-1", "hash", Now.AddMinutes(-5), Now.AddDays(30), RevokedAt: Now.AddMinutes(-1));

        Assert.True(token.IsRevoked);
        Assert.False(token.IsActive(Now));
    }
}
