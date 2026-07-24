namespace Rojan.Server.Domain.Authentication;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. One issued
/// refresh token, tracked so it can be looked up again on
/// <c>IAuthenticationService.RefreshAsync</c> and rotated (revoked +
/// replaced) rather than reused indefinitely - single-use rotation is
/// what makes a stolen-but-already-used token detectable (it shows up
/// already <see cref="IsRevoked"/>). <see cref="TokenHash"/> is a
/// (fast, since the token itself is already high-entropy random data -
/// see <c>Infrastructure.Security.JwtTokenService</c>'s own doc comment)
/// hash of the raw token value, never the raw value itself - the same
/// "never store the literal secret" principle
/// <see cref="User.PasswordHash"/> already establishes, applied here for
/// a different reason (a stolen database dump must not itself be a usable
/// set of bearer tokens).
/// </summary>
public sealed record RefreshToken(
    string Id,
    string UserId,
    string TokenHash,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt = null)
{
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsRevoked => RevokedAt is not null;

    public bool IsActive(DateTimeOffset now) => !IsRevoked && !IsExpired(now);
}
