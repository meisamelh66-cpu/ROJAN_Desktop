namespace Rojan.Server.Domain.Authentication;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. The tenant
/// boundary - every <see cref="Branch"/> and <see cref="User"/> belongs to
/// exactly one <see cref="Organization"/>, and nothing in this schema ever
/// lets a query cross that boundary implicitly (see
/// <see cref="UserRules.IsValidBranchAssignment"/>). Created as part of
/// <c>Application.Authentication.IAuthenticationService</c>'s "register
/// organization owner" flow - there is no separate "create organization"
/// operation in this commit, since standalone tenant management is out of
/// scope (see the solution's own README).
///
/// Sprint 8 Commit 3: Multi-Tenant Organization Foundation.
/// <see cref="Status"/> added - see <see cref="OrganizationStatus"/>'s own
/// doc comment and <see cref="OrganizationRules"/> for valid transitions.
/// <see cref="IsActive"/> is what <c>Application.Tenancy.ITenantService</c>
/// checks before trusting a request's tenant context.
/// </summary>
public sealed record Organization(string Id, string Name, OrganizationStatus Status, DateTimeOffset CreatedAt)
{
    public bool IsActive => Status == OrganizationStatus.Active;
}
