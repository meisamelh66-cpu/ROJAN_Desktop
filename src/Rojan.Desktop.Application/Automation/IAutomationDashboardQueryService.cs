namespace Rojan.Desktop.Application.Automation;

/// <summary>Requirement 32.12's Automation Dashboard summary numbers - workflow count, today's executions, failures, success rate, average execution time.</summary>
public sealed record AutomationDashboardSummaryDto(
    int TotalWorkflows,
    int PublishedWorkflows,
    int ExecutionsToday,
    int FailuresToday,
    double SuccessRatePercent,
    double AverageExecutionDurationMs,
    int PendingApprovals);

public interface IAutomationDashboardQueryService
{
    public Task<AutomationDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
