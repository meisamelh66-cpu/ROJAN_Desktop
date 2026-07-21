namespace Rojan.Desktop.Domain.Automation;

/// <summary>Pure derivation logic over a <see cref="WorkflowExecution"/>/its <see cref="WorkflowStepExecutionLog"/>s - duration, overall status, and terminal-state checks.</summary>
public static class WorkflowExecutionRules
{
    public static long ComputeDurationMs(DateTimeOffset startedAt, DateTimeOffset completedAt) =>
        Math.Max(0, (long)(completedAt - startedAt).TotalMilliseconds);

    /// <summary>Terminal statuses that will never transition further - <see cref="WorkflowExecutionStatus.Completed"/>, <see cref="WorkflowExecutionStatus.Failed"/>, and <see cref="WorkflowExecutionStatus.Cancelled"/>.</summary>
    public static bool IsTerminal(WorkflowExecutionStatus status) =>
        status is WorkflowExecutionStatus.Completed or WorkflowExecutionStatus.Failed or WorkflowExecutionStatus.Cancelled;

    /// <summary>The overall status implied by a set of step logs: any Failed step (after exhausting retries) fails the whole run; any step still Pending/Running means the run is still <see cref="WorkflowExecutionStatus.Running"/>; a step waiting on <see cref="WorkflowStepType.Approval"/> is reported separately by the engine as <see cref="WorkflowExecutionStatus.Waiting"/>, not derived here. Otherwise, every step Completed or Skipped means the run <see cref="WorkflowExecutionStatus.Completed"/>.</summary>
    public static WorkflowExecutionStatus DeriveStatus(IReadOnlyList<WorkflowStepExecutionLog> stepLogs)
    {
        if (stepLogs.Any(log => log.Status == StepExecutionStatus.Failed))
        {
            return WorkflowExecutionStatus.Failed;
        }

        if (stepLogs.Any(log => log.Status is StepExecutionStatus.Pending or StepExecutionStatus.Running))
        {
            return WorkflowExecutionStatus.Running;
        }

        return WorkflowExecutionStatus.Completed;
    }
}
