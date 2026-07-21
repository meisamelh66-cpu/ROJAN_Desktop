namespace Rojan.Desktop.Domain.Automation;

/// <summary>
/// A named, versioned automation workflow, as returned by
/// <see cref="IWorkflowRepository"/>. <see cref="Version"/>/
/// <see cref="ParentWorkflowId"/> implement Requirement 32.9
/// (Versioning): publishing a Draft snapshots it as a new immutable
/// record with an incremented <see cref="Version"/>, and rolling back
/// creates a fresh Draft copied from an older
/// <see cref="WorkflowStatus.Archived"/>/<see cref="WorkflowStatus.Published"/>
/// version - <see cref="ParentWorkflowId"/> links every version back to the
/// original workflow's id so its full history can be listed. Scoped to
/// <see cref="OrganizationId"/>/<see cref="BranchId"/>, same required-
/// never-defaulted scoping <c>Customers.Customer</c> established in Phase
/// 22A.
/// </summary>
public sealed record WorkflowDefinition(
    string Id,
    string ParentWorkflowId,
    string Name,
    string Description,
    IReadOnlyList<WorkflowStep> Steps,
    IReadOnlyList<TriggerType> TriggerTypes,
    WorkflowStatus Status,
    int Version,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CreatedByUserId,
    string OrganizationId,
    string BranchId);
