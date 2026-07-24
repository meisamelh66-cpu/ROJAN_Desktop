namespace Rojan.Server.Domain.Authentication;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. The one role
/// this commit's registration flow ever assigns -
/// <c>Application.Authentication.IAuthenticationService</c>'s "register
/// organization owner" operation stamps every new user with
/// <see cref="Owner"/>. Deliberately a plain string constant, not an enum
/// with every eventual role (staff/manager/etc.) or a permissions matrix -
/// both are explicitly out of scope for this commit (see the solution's
/// own README); this exists only so "Owner" is spelled the same way
/// everywhere it appears rather than repeated as a magic string.
/// </summary>
public static class UserRoles
{
    public const string Owner = "Owner";
}
