namespace Rojan.Server.Application.Authentication;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. The three
/// operations this commit implements - registering a brand-new tenant's
/// first user, logging an existing user in, and rotating a refresh token.
/// No invite-an-existing-tenant's-user, no password reset, no logout/
/// revoke-all - all explicitly out of scope for a foundation commit. Every
/// operation returns the same <see cref="AuthenticationResult"/> shape,
/// since all three end with the caller holding a fresh, usable token pair.
/// </summary>
public interface IAuthenticationService
{
    public Task<AuthenticationResult> RegisterOrganizationOwnerAsync(RegisterOrganizationOwnerRequest request, CancellationToken cancellationToken = default);

    public Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    public Task<AuthenticationResult> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
}
