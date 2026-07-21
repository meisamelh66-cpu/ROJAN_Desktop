namespace Rojan.Desktop.Domain.Automation;

/// <summary>Repository abstraction for workflow execution history (Requirement 32.8/32.10). Domain defines the contract; Infrastructure provides the concrete implementation (local JSON persistence, capped like every other bounded-history store in this app).</summary>
public interface IWorkflowExecutionRepository
{
    public Task<IReadOnlyList<WorkflowExecution>> GetAllAsync(CancellationToken cancellationToken = default);

    public Task<WorkflowExecution?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    public Task SaveAsync(WorkflowExecution execution, CancellationToken cancellationToken = default);
}
