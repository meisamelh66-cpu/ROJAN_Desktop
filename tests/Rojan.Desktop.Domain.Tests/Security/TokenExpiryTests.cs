using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Domain.Tests.Security;

public sealed class TokenExpiryTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AuthToken_IsExpired_BeforeExpiry_ReturnsFalse()
    {
        var token = new AuthToken("value", Now.AddMinutes(-30), Now.AddMinutes(30));

        Assert.False(token.IsExpired(Now));
    }

    [Fact]
    public void AuthToken_IsExpired_AfterExpiry_ReturnsTrue()
    {
        var token = new AuthToken("value", Now.AddHours(-2), Now.AddHours(-1));

        Assert.True(token.IsExpired(Now));
    }

    [Fact]
    public void RefreshToken_IsExpired_BeforeExpiry_ReturnsFalse()
    {
        var token = new RefreshToken("value", Now.AddDays(-1), Now.AddDays(29));

        Assert.False(token.IsExpired(Now));
    }

    [Fact]
    public void RefreshToken_IsExpired_AfterExpiry_ReturnsTrue()
    {
        var token = new RefreshToken("value", Now.AddDays(-31), Now.AddDays(-1));

        Assert.True(token.IsExpired(Now));
    }
}
