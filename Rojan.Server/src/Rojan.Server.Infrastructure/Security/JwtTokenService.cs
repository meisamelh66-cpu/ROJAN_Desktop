using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Rojan.Server.Application.Authentication;
using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Infrastructure.Security;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. Default
/// <see cref="ITokenService"/>:
///
/// - <see cref="GenerateAccessToken"/> issues a signed JWT
///   (HMAC-SHA256, <c>System.IdentityModel.Tokens.Jwt</c> - Microsoft's
///   own JWT library, the same one every ASP.NET Core JWT bearer sample
///   uses) carrying <see cref="JwtRegisteredClaimNames.Sub"/> (user id),
///   email, a custom <c>org_id</c> claim, an optional custom
///   <c>branch_id</c> claim, and a <see cref="ClaimTypes.Role"/> claim -
///   this is what lets a future <c>[Authorize]</c>-protected endpoint
///   read tenant context straight off <see cref="ClaimsPrincipal"/>
///   without a database round-trip.
/// - <see cref="GenerateRefreshTokenValue"/> is unrelated to JWTs
///   entirely - just 256 bits of <see cref="RandomNumberGenerator"/>
///   output, the same opaque-random-bearer-token approach the desktop
///   solution's own <c>LocalSessionService.GenerateTokenValue</c> already
///   uses (a refresh token needs no embedded claims - it is only ever
///   looked up, never decoded).
/// - <see cref="HashRefreshTokenValue"/> is a plain SHA-256 hash (not
///   PBKDF2/BCrypt like <see cref="Pbkdf2PasswordHasher"/>) - deliberately
///   fast, because a refresh token is already 256 bits of high-entropy
///   random data, not a low-entropy human-chosen password; slow hashing
///   defends against brute-forcing a small guess space, which does not
///   apply here.
/// - <see cref="ValidateAccessToken"/> checks signature, issuer, audience,
///   and expiry (with a small <see cref="ClockSkew"/> tolerance) -
///   returns <see langword="null"/> rather than throwing for any
///   validation failure, since an invalid/expired token is an ordinary,
///   expected outcome for a bearer-token API, not a bug.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("org_id", user.OrganizationId),
            new(ClaimTypes.Role, user.Role),
        };

        if (user.BranchId is not null)
        {
            claims.Add(new Claim("branch_id", user.BranchId));
        }

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            notBefore: now,
            expires: now.AddMinutes(_options.AccessTokenLifetimeMinutes),
            signingCredentials: new SigningCredentials(SigningKey(), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshTokenValue() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public string HashRefreshTokenValue(string rawRefreshToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken)));

    public ClaimsPrincipal? ValidateAccessToken(string accessToken)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = SigningKey(),
            ValidateLifetime = true,
            ClockSkew = ClockSkew,
        };

        try
        {
            // MapInboundClaims = false: without this, JwtSecurityTokenHandler
            // silently rewrites short claim names ("sub", "email") to long
            // legacy ClaimTypes URIs on the way out, so a caller reading
            // JwtRegisteredClaimNames.Sub back off the resulting
            // ClaimsPrincipal would find nothing there - the claims must
            // round-trip with the exact names GenerateAccessToken wrote.
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            return handler.ValidateToken(accessToken, parameters, out _);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private SymmetricSecurityKey SigningKey() => new(Encoding.UTF8.GetBytes(_options.SigningKey));
}
