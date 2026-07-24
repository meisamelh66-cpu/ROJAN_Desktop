using Rojan.Server.Application.Authentication;
using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Application.Tests.Authentication;

/// <summary>
/// Exercises <see cref="AuthenticationService"/>'s orchestration against
/// fakes (see <c>FakeAuthenticationRepositories</c>/<c>FakeSecurityServices</c>)
/// - real password hashing/JWT correctness is covered separately in
/// <c>Infrastructure.Tests.Security</c>.
/// </summary>
public sealed class AuthenticationServiceTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeOrganizationRepository _organizationRepository = new();
    private readonly FakeRefreshTokenRepository _refreshTokenRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeTokenService _tokenService = new();

    private AuthenticationService CreateSut() =>
        new(_userRepository, _organizationRepository, _refreshTokenRepository, _passwordHasher, _tokenService);

    [Fact]
    public async Task RegisterOrganizationOwnerAsync_NewEmail_CreatesOrganizationAndOwnerUser()
    {
        var sut = CreateSut();
        var request = new RegisterOrganizationOwnerRequest("Rojan Salon", "owner@rojan.example", "SuperSecret1", "Noah Bennett");

        var result = await sut.RegisterOrganizationOwnerAsync(request);

        var organization = Assert.Single(_organizationRepository.Organizations);
        Assert.Equal("Rojan Salon", organization.Name);

        var user = Assert.Single(_userRepository.Users);
        Assert.Equal("owner@rojan.example", user.Email);
        Assert.Equal(organization.Id, user.OrganizationId);
        Assert.Null(user.BranchId);
        Assert.Equal(UserRoles.Owner, user.Role);
        Assert.Equal("hashed:SuperSecret1", user.PasswordHash);

        Assert.Equal(organization.Id, result.OrganizationId);
        Assert.Null(result.BranchId);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal([UserRoles.Owner], result.Roles);
        Assert.Equal($"access-token-for-{user.Id}", result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
    }

    [Fact]
    public async Task RegisterOrganizationOwnerAsync_EmailAlreadyRegistered_ThrowsAndCreatesNothing()
    {
        var sut = CreateSut();
        _userRepository.Users.Add(new User("existing-user", "org-1", null, "owner@rojan.example", "hash", "Existing Owner", UserRoles.Owner, DateTimeOffset.UtcNow));
        var request = new RegisterOrganizationOwnerRequest("Rojan Salon", "owner@rojan.example", "SuperSecret1", "Noah Bennett");

        await Assert.ThrowsAsync<EmailAlreadyRegisteredException>(() => sut.RegisterOrganizationOwnerAsync(request));

        Assert.Empty(_organizationRepository.Organizations);
        Assert.Single(_userRepository.Users);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokensWithTenantContext()
    {
        var sut = CreateSut();
        var user = new User("user-1", "org-1", "branch-1", "owner@rojan.example", "hashed:SuperSecret1", "Noah Bennett", UserRoles.Owner, DateTimeOffset.UtcNow);
        _userRepository.Users.Add(user);

        var result = await sut.LoginAsync(new LoginRequest("owner@rojan.example", "SuperSecret1"));

        Assert.Equal("org-1", result.OrganizationId);
        Assert.Equal("branch-1", result.BranchId);
        Assert.Equal("user-1", result.UserId);
        Assert.Equal([UserRoles.Owner], result.Roles);
        Assert.Equal("access-token-for-user-1", result.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsInvalidCredentialsException()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => sut.LoginAsync(new LoginRequest("nobody@rojan.example", "whatever")));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsInvalidCredentialsException()
    {
        var sut = CreateSut();
        _userRepository.Users.Add(new User("user-1", "org-1", null, "owner@rojan.example", "hashed:SuperSecret1", "Noah Bennett", UserRoles.Owner, DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => sut.LoginAsync(new LoginRequest("owner@rojan.example", "WrongPassword")));
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_ReturnsNewTokensAndRevokesTheOldOne()
    {
        var sut = CreateSut();
        var user = new User("user-1", "org-1", null, "owner@rojan.example", "hashed:SuperSecret1", "Noah Bennett", UserRoles.Owner, DateTimeOffset.UtcNow);
        _userRepository.Users.Add(user);
        var now = DateTimeOffset.UtcNow;
        var existingToken = new RefreshToken("refresh-1", "user-1", "hash-of-raw-refresh-token", now.AddDays(-1), now.AddDays(29));
        _refreshTokenRepository.Tokens.Add(existingToken);

        var result = await sut.RefreshAsync(new RefreshTokenRequest("raw-refresh-token"));

        Assert.Equal("user-1", result.UserId);
        Assert.Equal("access-token-for-user-1", result.AccessToken);

        var oldToken = _refreshTokenRepository.Tokens.Single(token => token.Id == "refresh-1");
        Assert.True(oldToken.IsRevoked);

        // A new token was issued in addition to the (now-revoked) old one.
        Assert.Equal(2, _refreshTokenRepository.Tokens.Count);
    }

    [Fact]
    public async Task RefreshAsync_UnknownToken_ThrowsInvalidRefreshTokenException()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => sut.RefreshAsync(new RefreshTokenRequest("never-issued")));
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_ThrowsInvalidRefreshTokenException()
    {
        var sut = CreateSut();
        var now = DateTimeOffset.UtcNow;
        _refreshTokenRepository.Tokens.Add(new RefreshToken("refresh-1", "user-1", "hash-of-expired-token", now.AddDays(-31), now.AddDays(-1)));

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => sut.RefreshAsync(new RefreshTokenRequest("expired-token")));
    }

    [Fact]
    public async Task RefreshAsync_AlreadyRevokedToken_ThrowsInvalidRefreshTokenException()
    {
        // Replay protection: a token already consumed by one rotation must
        // never be usable again, even though it has not technically expired.
        var sut = CreateSut();
        var now = DateTimeOffset.UtcNow;
        _refreshTokenRepository.Tokens.Add(new RefreshToken("refresh-1", "user-1", "hash-of-revoked-token", now.AddDays(-1), now.AddDays(29), RevokedAt: now.AddMinutes(-1)));

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => sut.RefreshAsync(new RefreshTokenRequest("revoked-token")));
    }
}
