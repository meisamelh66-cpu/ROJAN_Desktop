namespace Rojan.Desktop.Domain.Security;

/// <summary>Phase 25: Secure Authentication Foundation. Long-lived credential used to obtain a new <see cref="AuthToken"/> without re-authenticating - see <see cref="AuthToken"/>'s own doc comment for why <see cref="Value"/> stays opaque.</summary>
public sealed record RefreshToken(string Value, DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt)
{
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
}
