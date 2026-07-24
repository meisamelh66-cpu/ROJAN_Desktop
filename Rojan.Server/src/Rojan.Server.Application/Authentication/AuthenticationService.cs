using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Application.Authentication;

/// <summary>
/// Default <see cref="IAuthenticationService"/>. Pure orchestration - no
/// hashing/JWT/persistence logic of its own, only calling
/// <see cref="IPasswordHasher"/>/<see cref="ITokenService"/>/the
/// repositories in the right order and translating the outcome into
/// either an <see cref="AuthenticationResult"/> or the right
/// <see cref="AuthenticationException"/> subtype.
///
/// <see cref="RefreshAsync"/> rotates: the presented refresh token is
/// revoked immediately once it is found valid, before a new pair is ever
/// issued - a single-use token, not a long-lived credential re-checked on
/// every call. This is what makes a stolen-but-already-used refresh
/// token detectable rather than silently exploitable: replaying it after
/// the legitimate client already rotated it finds an already-revoked
/// token and is rejected the same as any other invalid one.
/// </summary>
public sealed class AuthenticationService : IAuthenticationService
{
    /// <summary>Matches the desktop solution's own <c>LocalSessionService.RefreshTokenLifetime</c> - not a coincidence, a deliberately consistent default across both the desktop's local session and this backend's server-issued one.</summary>
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    private readonly IUserRepository _userRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthenticationService(
        IUserRepository userRepository,
        IOrganizationRepository organizationRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _organizationRepository = organizationRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthenticationResult> RegisterOrganizationOwnerAsync(RegisterOrganizationOwnerRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken).ConfigureAwait(true);
        if (existing is not null)
        {
            throw new EmailAlreadyRegisteredException();
        }

        var now = DateTimeOffset.UtcNow;

        var organization = new Organization(Guid.NewGuid().ToString(), request.OrganizationName, now);
        await _organizationRepository.CreateAsync(organization, cancellationToken).ConfigureAwait(true);

        var user = new User(
            Guid.NewGuid().ToString(),
            organization.Id,
            BranchId: null,
            request.Email,
            _passwordHasher.Hash(request.Password),
            request.FullName,
            UserRoles.Owner,
            now);
        await _userRepository.CreateAsync(user, cancellationToken).ConfigureAwait(true);

        return await IssueTokensAsync(user, cancellationToken).ConfigureAwait(true);
    }

    public async Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken).ConfigureAwait(true);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            // Deliberately the same exception for "no such account" and
            // "wrong password" - see InvalidCredentialsException's own doc
            // comment.
            throw new InvalidCredentialsException();
        }

        return await IssueTokensAsync(user, cancellationToken).ConfigureAwait(true);
    }

    public async Task<AuthenticationResult> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenService.HashRefreshTokenValue(request.RefreshToken);
        var stored = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken).ConfigureAwait(true);

        var now = DateTimeOffset.UtcNow;
        if (stored is null || !stored.IsActive(now))
        {
            throw new InvalidRefreshTokenException();
        }

        await _refreshTokenRepository.RevokeAsync(stored.Id, now, cancellationToken).ConfigureAwait(true);

        var user = await _userRepository.GetByIdAsync(stored.UserId, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidRefreshTokenException();

        return await IssueTokensAsync(user, cancellationToken).ConfigureAwait(true);
    }

    private async Task<AuthenticationResult> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var rawRefreshToken = _tokenService.GenerateRefreshTokenValue();

        var now = DateTimeOffset.UtcNow;
        var refreshToken = new RefreshToken(
            Guid.NewGuid().ToString(),
            user.Id,
            _tokenService.HashRefreshTokenValue(rawRefreshToken),
            now,
            now + RefreshTokenLifetime);
        await _refreshTokenRepository.CreateAsync(refreshToken, cancellationToken).ConfigureAwait(true);

        return new AuthenticationResult(accessToken, rawRefreshToken, user.OrganizationId, user.BranchId, user.Id, [user.Role]);
    }
}
