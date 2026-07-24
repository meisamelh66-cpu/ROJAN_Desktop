using System.Security.Claims;
using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Application.Authentication;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. Everything
/// <c>AuthenticationService</c> needs to issue and validate tokens,
/// without knowing anything about JWTs, signing keys, or hashing
/// algorithms - all of that lives in the concrete implementation
/// (<c>Infrastructure.Security.JwtTokenService</c>, see its own doc
/// comment). <see cref="GenerateAccessToken"/> is a signed, short-lived
/// JWT carrying tenant/role claims; <see cref="GenerateRefreshTokenValue"/>
/// is an opaque, high-entropy random string carrying no claims at all
/// (nothing to decode - it is only ever looked up by its
/// <see cref="HashRefreshTokenValue"/> hash against
/// <c>Domain.Authentication.IRefreshTokenRepository</c>).
/// </summary>
public interface ITokenService
{
    public string GenerateAccessToken(User user);

    public string GenerateRefreshTokenValue();

    /// <summary>Hashes a raw refresh token value for storage/lookup - see <c>Domain.Authentication.RefreshToken.TokenHash</c>'s own doc comment for why only the hash is ever persisted.</summary>
    public string HashRefreshTokenValue(string rawRefreshToken);

    /// <summary>Validates a JWT's signature and expiry, returning the resulting <see cref="ClaimsPrincipal"/> - or <see langword="null"/> if the token is invalid/expired/tampered for any reason. Never throws for an invalid token; an invalid token is an expected, ordinary outcome here, not an exceptional one.</summary>
    public ClaimsPrincipal? ValidateAccessToken(string accessToken);
}
