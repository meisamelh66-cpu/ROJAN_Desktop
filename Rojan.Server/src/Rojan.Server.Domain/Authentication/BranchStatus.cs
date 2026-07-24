namespace Rojan.Server.Domain.Authentication;

/// <summary>Sprint 8 Commit 3: Multi-Tenant Organization Foundation. The lifecycle of a <see cref="Branch"/> - same reasoning as <see cref="OrganizationStatus"/>'s own doc comment, one level down the tenant hierarchy (an organization can deactivate one branch without suspending itself).</summary>
public enum BranchStatus
{
    Active,
    Inactive,
}
