namespace Rojan.Desktop.Application.Automation;

/// <summary>
/// Workflow CRUD plus Requirement 32.9's versioning lifecycle
/// (Draft -&gt; Published -&gt; Archived, rollback-ready). Every mutating
/// method returns/persists a <see cref="WorkflowDefinitionDto"/> - the
/// step-graph structure itself is validated via
/// <c>Domain.Automation.WorkflowRules</c> before it can ever be published.
/// </summary>
public interface IWorkflowService
{
    /// <summary>Every workflow record across every version/status.</summary>
    public Task<IReadOnlyList<WorkflowDefinitionDto>> GetAllAsync(CancellationToken cancellationToken = default);

    public Task<WorkflowDefinitionDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Every version recorded under the same lineage, newest first - Requirement 32.9's version history.</summary>
    public Task<IReadOnlyList<WorkflowDefinitionDto>> GetVersionsAsync(string parentWorkflowId, CancellationToken cancellationToken = default);

    /// <summary>Every currently-<see cref="WorkflowStatus.Published"/> and enabled workflow - what the Trigger Engine and Scheduled Jobs actually run.</summary>
    public Task<IReadOnlyList<WorkflowDefinitionDto>> GetPublishedAsync(CancellationToken cancellationToken = default);

    /// <summary>Structural problems with <paramref name="steps"/> (missing Start/End, dangling references, unreachable steps) - empty means valid. Pure, no I/O.</summary>
    public IReadOnlyList<string> Validate(IReadOnlyList<WorkflowStepDto> steps);

    public Task<WorkflowDefinitionDto> CreateDraftAsync(
        string name,
        string description,
        IReadOnlyList<WorkflowStepDto> steps,
        IReadOnlyList<TriggerType> triggerTypes,
        string createdByUserId,
        string organizationId,
        string branchId,
        CancellationToken cancellationToken = default);

    /// <summary>Updates an existing Draft's content in place. Throws <see cref="InvalidOperationException"/> if the workflow is not currently a Draft - a Published/Archived version is immutable, per Requirement 32.9.</summary>
    public Task<WorkflowDefinitionDto> SaveDraftAsync(WorkflowDefinitionDto workflow, CancellationToken cancellationToken = default);

    /// <summary>Validates and publishes a Draft, archiving whatever version was previously Published under the same lineage (a lineage has at most one Published version at a time). Throws if the workflow is not a Draft or fails <see cref="Validate"/>.</summary>
    public Task<WorkflowDefinitionDto> PublishAsync(string workflowId, CancellationToken cancellationToken = default);

    public Task ArchiveAsync(string workflowId, CancellationToken cancellationToken = default);

    /// <summary>Creates a new Draft copied from an earlier version under the same lineage - Requirement 32.9's "Rollback-ready". The new Draft still needs <see cref="PublishAsync"/> to actually take effect.</summary>
    public Task<WorkflowDefinitionDto> RollbackAsync(string parentWorkflowId, int toVersion, string userId, CancellationToken cancellationToken = default);

    public Task DeleteAsync(string workflowId, CancellationToken cancellationToken = default);
}
