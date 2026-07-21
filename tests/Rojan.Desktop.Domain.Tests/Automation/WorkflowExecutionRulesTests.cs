using Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Domain.Tests.Automation;

/// <summary>Exercises <see cref="WorkflowExecutionRules"/> - duration/status derivation.</summary>
public sealed class WorkflowExecutionRulesTests
{
    [Fact]
    public void ComputeDurationMs_ReturnsElapsedMilliseconds()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddSeconds(2);

        Assert.Equal(2000, WorkflowExecutionRules.ComputeDurationMs(start, end));
    }

    [Fact]
    public void ComputeDurationMs_NeverNegative()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 1, TimeSpan.Zero);
        var end = start.AddSeconds(-5);

        Assert.Equal(0, WorkflowExecutionRules.ComputeDurationMs(start, end));
    }

    [Theory]
    [InlineData(WorkflowExecutionStatus.Completed, true)]
    [InlineData(WorkflowExecutionStatus.Failed, true)]
    [InlineData(WorkflowExecutionStatus.Cancelled, true)]
    [InlineData(WorkflowExecutionStatus.Running, false)]
    [InlineData(WorkflowExecutionStatus.Waiting, false)]
    public void IsTerminal_ReflectsTerminalStatuses(WorkflowExecutionStatus status, bool expected)
    {
        Assert.Equal(expected, WorkflowExecutionRules.IsTerminal(status));
    }

    private static WorkflowStepExecutionLog Log(StepExecutionStatus status) =>
        new("step-1", "Step", WorkflowStepType.Notification, status, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 1, null);

    [Fact]
    public void DeriveStatus_AnyFailedStep_ReturnsFailed()
    {
        var logs = new[] { Log(StepExecutionStatus.Completed), Log(StepExecutionStatus.Failed) };

        Assert.Equal(WorkflowExecutionStatus.Failed, WorkflowExecutionRules.DeriveStatus(logs));
    }

    [Fact]
    public void DeriveStatus_StillPendingStep_ReturnsRunning()
    {
        var logs = new[] { Log(StepExecutionStatus.Completed), Log(StepExecutionStatus.Pending) };

        Assert.Equal(WorkflowExecutionStatus.Running, WorkflowExecutionRules.DeriveStatus(logs));
    }

    [Fact]
    public void DeriveStatus_AllCompletedOrSkipped_ReturnsCompleted()
    {
        var logs = new[] { Log(StepExecutionStatus.Completed), Log(StepExecutionStatus.Skipped) };

        Assert.Equal(WorkflowExecutionStatus.Completed, WorkflowExecutionRules.DeriveStatus(logs));
    }
}
