namespace Rojan.Desktop.Domain.Organizations;

/// <summary>The dedicated workspace a session is currently operating as - one per role this phase's "WORKSPACES" requirement names, each with its own fixed <see cref="Permission"/> set via <see cref="RolePermissions"/>.</summary>
public enum WorkspaceRole
{
    PlatformOwner,
    OrganizationOwner,
    OrganizationManager,
    BranchManager,
    Reception,
    Specialist,
    Inventory,
    Accounting,
    Hr,
    Ai,
    Support,
    Marketing,

    /// <summary>
    /// Remediation Phase 2 (Role Mapping Hardening): the deliberate,
    /// fail-closed fallback for a session whose real backend role/
    /// relationship could not be recognized as one of the roles above -
    /// see <c>Application.Salons.SalonSessionAdapter.ToWorkspaceRole</c>'s
    /// own doc comment for the exact cases that map here. Deliberately
    /// absent from <see cref="RolePermissions"/>'s map (or present with an
    /// explicit empty set - see that class's own comment) so it grants
    /// zero permissions, not even <see cref="Permission.DashboardView"/> -
    /// "deny safely" means every capability, no exceptions.
    /// </summary>
    Unknown,
}
