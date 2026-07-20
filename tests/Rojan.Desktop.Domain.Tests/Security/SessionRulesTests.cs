using Rojan.Desktop.Domain.Identity;
using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Domain.Tests.Security;

public sealed class SessionRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DetermineState_NullSession_ReturnsSignedOut()
    {
        var result = SessionRules.DetermineState(null, Now);

        Assert.Equal(AuthenticationState.SignedOut, result);
    }

    [Fact]
    public void DetermineState_SessionNotYetExpired_ReturnsAuthenticated()
    {
        var session = new SessionIdentity("session-1", "user-1", "device-1", Now.AddHours(-1), Now.AddHours(1));

        var result = SessionRules.DetermineState(session, Now);

        Assert.Equal(AuthenticationState.Authenticated, result);
    }

    [Fact]
    public void DetermineState_SessionPastExpiry_ReturnsExpired()
    {
        var session = new SessionIdentity("session-1", "user-1", "device-1", Now.AddHours(-2), Now.AddHours(-1));

        var result = SessionRules.DetermineState(session, Now);

        Assert.Equal(AuthenticationState.Expired, result);
    }

    [Fact]
    public void DetermineState_SessionExpiringExactlyNow_ReturnsExpired()
    {
        var session = new SessionIdentity("session-1", "user-1", "device-1", Now.AddHours(-1), Now);

        var result = SessionRules.DetermineState(session, Now);

        Assert.Equal(AuthenticationState.Expired, result);
    }

    [Fact]
    public void IsExpiringSoon_WithinWindow_ReturnsTrue()
    {
        var session = new SessionIdentity("session-1", "user-1", "device-1", Now.AddHours(-1), Now.Add(SessionRules.ExpiringSoonWindow).AddSeconds(-1));

        Assert.True(SessionRules.IsExpiringSoon(session, Now));
    }

    [Fact]
    public void IsExpiringSoon_WellBeforeExpiry_ReturnsFalse()
    {
        var session = new SessionIdentity("session-1", "user-1", "device-1", Now.AddHours(-1), Now.AddDays(1));

        Assert.False(SessionRules.IsExpiringSoon(session, Now));
    }

    [Fact]
    public void IsExpiringSoon_AlreadyExpired_ReturnsFalse()
    {
        var session = new SessionIdentity("session-1", "user-1", "device-1", Now.AddHours(-2), Now.AddHours(-1));

        Assert.False(SessionRules.IsExpiringSoon(session, Now));
    }
}
