using DomainAutomation = Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Application.Automation;

/// <summary>Default <see cref="ITriggerEngine"/>. Finds every enabled Published workflow subscribed to the raised trigger and starts a run of each via <see cref="IWorkflowExecutionEngine"/> - sequential, not parallel, so execution order matches workflow list order and one workflow's failure never races another's.</summary>
public sealed class TriggerEngine : ITriggerEngine
{
    private readonly DomainAutomation.IWorkflowRepository _workflowRepository;
    private readonly IWorkflowExecutionEngine _executionEngine;

    public TriggerEngine(DomainAutomation.IWorkflowRepository workflowRepository, IWorkflowExecutionEngine executionEngine)
    {
        _workflowRepository = workflowRepository;
        _executionEngine = executionEngine;
    }

    public async Task<IReadOnlyList<WorkflowExecutionDto>> RaiseAsync(
        TriggerType trigger,
        IReadOnlyDictionary<string, string> facts,
        string organizationId,
        string branchId,
        string triggeredByUserId,
        CancellationToken cancellationToken = default)
    {
        var domainTrigger = AutomationMapping.MapToDomain(trigger);
        var allWorkflows = await _workflowRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var matching = allWorkflows
            .Where(workflow => workflow.Status == DomainAutomation.WorkflowStatus.Published
                && workflow.IsEnabled
                && workflow.TriggerTypes.Contains(domainTrigger))
            .ToList();

        var results = new List<WorkflowExecutionDto>();
        foreach (var workflow in matching)
        {
            var execution = await _executionEngine.ExecuteAsync(workflow.Id, trigger, triggeredByUserId, facts, organizationId, branchId, cancellationToken).ConfigureAwait(false);
            results.Add(execution);
        }

        return results;
    }
}
