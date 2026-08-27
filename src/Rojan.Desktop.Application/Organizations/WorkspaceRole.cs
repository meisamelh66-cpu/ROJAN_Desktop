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
    Marketing,

    /// <summary>
    /// Remediation Phase 2 (Role Mapping Hardening): mirrors
    /// <see cref="Domain.Organizations.WorkspaceRole.Unknown"/> - see its
    /// own doc comment. Application's own copy, same reasoning as every
    /// other member here.
    /// </summary>
    Unknown,
}
