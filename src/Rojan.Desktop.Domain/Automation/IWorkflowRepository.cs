namespace Rojan.Desktop.Domain.Automation;

/// <summary>Repository abstraction for workflow definitions, including every version (Requirement 32.9). Domain defines the contract; Infrastructure provides the concrete implementation (local JSON persistence).</summary>
public interface IWorkflowRepository
{
    /// <summary>Every workflow record across every version/status - a caller filters (e.g. "latest Published per ParentWorkflowId") as needed.</summary>
    public Task<IReadOnlyList<WorkflowDefinition>> GetAllAsync(CancellationToken cancellationToken = default);

    public Task<WorkflowDefinition?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Every version recorded under the same <see cref="WorkflowDefinition.ParentWorkflowId"/>, newest first.</summary>
    public Task<IReadOnlyList<WorkflowDefinition>> GetVersionsAsync(string parentWorkflowId, CancellationToken cancellationToken = default);

    public Task SaveAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default);

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
