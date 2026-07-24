namespace Rojan.Server.Application.Authentication;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. Returned by
/// every <c>IAuthenticationService</c> operation (register/login/refresh)
/// - always carries full tenant context (<see cref="OrganizationId"/>/
/// <see cref="BranchId"/>) alongside the tokens themselves, since a
/// client of this backend must always know which tenant it is now acting
/// as, not just who it is. <see cref="Roles"/> wraps
/// <c>Domain.Authentication.User.Role</c>'s single value in a list - see
/// that record's own doc comment for why the wire shape is already
/// multi-role-ready even though the underlying model isn't yet.
/// </summary>
public sealed record AuthenticationResult(
    string AccessToken,
    string RefreshToken,
    string OrganizationId,
    string? BranchId,
    string UserId,
    IReadOnlyList<string> Roles);
