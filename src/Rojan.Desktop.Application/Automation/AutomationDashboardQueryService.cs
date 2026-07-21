using DomainAutomation = Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Application.Automation;

/// <summary>Default <see cref="IAutomationDashboardQueryService"/>. Aggregates live from the workflow/execution/approval repositories every call - no caching, the same documented seam every other in-memory-backed query service in this app leaves for a future cache.</summary>
public sealed class AutomationDashboardQueryService : IAutomationDashboardQueryService
{
    private readonly DomainAutomation.IWorkflowRepository _workflowRepository;
    private readonly DomainAutomation.IWorkflowExecutionRepository _executionRepository;
    private readonly DomainAutomation.IApprovalRepository _approvalRepository;

    public AutomationDashboardQueryService(
        DomainAutomation.IWorkflowRepository workflowRepository,
        DomainAutomation.IWorkflowExecutionRepository executionRepository,
        DomainAutomation.IApprovalRepository approvalRepository)
    {
        _workflowRepository = workflowRepository;
        _executionRepository = executionRepository;
        _approvalRepository = approvalRepository;
    }

    public async Task<AutomationDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var workflows = await _workflowRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var executions = await _executionRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var approvals = await _approvalRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        var today = DateTimeOffset.UtcNow.UtcDateTime.Date;
        var todaysExecutions = executions.Where(execution => execution.StartedAt.UtcDateTime.Date == today).ToList();
        var failuresToday = todaysExecutions.Count(execution => execution.Status == DomainAutomation.WorkflowExecutionStatus.Failed);
        var completedToday = todaysExecutions.Where(execution => execution.Status == DomainAutomation.WorkflowExecutionStatus.Completed && execution.DurationMs.HasValue).ToList();

        var successRate = todaysExecutions.Count == 0 ? 0d : (double)(todaysExecutions.Count - failuresToday) / todaysExecutions.Count * 100d;
        var averageDuration = completedToday.Count == 0 ? 0d : completedToday.Average(execution => execution.DurationMs!.Value);

        return new AutomationDashboardSummaryDto(
            TotalWorkflows: workflows.Select(workflow => workflow.ParentWorkflowId).Distinct().Count(),
            PublishedWorkflows: workflows.Count(workflow => workflow.Status == DomainAutomation.WorkflowStatus.Published),
            ExecutionsToday: todaysExecutions.Count,
            FailuresToday: failuresToday,
            SuccessRatePercent: Math.Round(successRate, 1),
            AverageExecutionDurationMs: Math.Round(averageDuration, 0),
            PendingApprovals: approvals.Count(request => request.Status == DomainAutomation.ApprovalStatus.Pending));
    }
}
