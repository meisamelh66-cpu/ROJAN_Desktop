namespace Rojan.Desktop.Domain.Security;

/// <summary>
/// Phase 25: Secure Authentication Foundation. A short-lived access token
/// issued alongside a <see cref="RefreshToken"/> when a
/// <see cref="Identity.SessionIdentity"/> is created. <see cref="Value"/>
/// is opaque here by design (this bounded context does not need to parse
/// it, e.g. as a JWT) - whatever the future backend issues, the
/// abstraction only needs to carry it and know when it expires.
/// </summary>
public sealed record AuthToken(string Value, DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt)
{
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
}
