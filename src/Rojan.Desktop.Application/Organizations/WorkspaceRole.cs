namespace Rojan.Desktop.Application.Organizations;

/// <summary>Application's own copy of the workspace-role concept - see <see cref="Permission"/>'s doc comment for the mapping rationale.</summary>
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
}
