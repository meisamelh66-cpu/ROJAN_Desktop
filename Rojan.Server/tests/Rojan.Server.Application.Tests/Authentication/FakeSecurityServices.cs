using System.Security.Claims;
using Rojan.Server.Application.Authentication;
using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Application.Tests.Authentication;

/// <summary>Deterministic, non-cryptographic <see cref="IPasswordHasher"/> test double - fast and predictable, since these tests exercise <c>AuthenticationService</c>'s orchestration, not password-hashing correctness itself (that is <c>Infrastructure.Tests.Security.Pbkdf2PasswordHasherTests</c>'s job).</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string password, string hash) => hash == Hash(password);
}

/// <summary>Deterministic <see cref="ITokenService"/> test double - real JWT generation/validation is exercised by <c>Infrastructure.Tests.Security.JwtTokenServiceTests</c> instead.</summary>
internal sealed class FakeTokenService : ITokenService
{
    private int _refreshTokenCounter;

    public int GenerateAccessTokenCallCount { get; private set; }

    public string GenerateAccessToken(User user)
    {
        GenerateAccessTokenCallCount++;
        return $"access-token-for-{user.Id}";
    }

    public string GenerateRefreshTokenValue() => $"refresh-token-{++_refreshTokenCounter}";

    public string HashRefreshTokenValue(string rawRefreshToken) => $"hash-of-{rawRefreshToken}";

    public ClaimsPrincipal? ValidateAccessToken(string accessToken) =>
        throw new NotSupportedException("Not used by AuthenticationService directly - see JwtTokenServiceTests for validation coverage.");
}
