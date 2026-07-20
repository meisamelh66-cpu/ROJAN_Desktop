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
}
