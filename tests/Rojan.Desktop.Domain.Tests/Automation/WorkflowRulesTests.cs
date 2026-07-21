using Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Domain.Tests.Automation;

/// <summary>Exercises <see cref="WorkflowRules"/> - structural validation and step-graph traversal.</summary>
public sealed class WorkflowRulesTests
{
    private static WorkflowStep Step(string id, WorkflowStepType type, string? nextId = null, IReadOnlyDictionary<string, string>? branches = null) =>
        new(id, type, id, new Dictionary<string, string>(), nextId, branches);

    [Fact]
    public void Validate_EmptySteps_ReportsError()
    {
        var errors = WorkflowRules.Validate([]);

        Assert.Contains(errors, error => error.Contains("at least one step", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_NoStartStep_ReportsError()
    {
        var steps = new[] { Step("end", WorkflowStepType.End) };

        var errors = WorkflowRules.Validate(steps);

        Assert.Contains(errors, error => error.Contains("Start step", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_NoEndStep_ReportsError()
    {
        var steps = new[] { Step("start", WorkflowStepType.Start) };

        var errors = WorkflowRules.Validate(steps);

        Assert.Contains(errors, error => error.Contains("End step", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_DanglingNextStepId_ReportsError()
    {
        var steps = new[]
        {
            Step("start", WorkflowStepType.Start, "missing"),
            Step("end", WorkflowStepType.End),
        };

        var errors = WorkflowRules.Validate(steps);

        Assert.Contains(errors, error => error.Contains("does not exist", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_UnreachableStep_ReportsError()
    {
        var steps = new[]
        {
            Step("start", WorkflowStepType.Start, "end"),
            Step("end", WorkflowStepType.End),
            Step("orphan", WorkflowStepType.Notification),
        };

        var errors = WorkflowRules.Validate(steps);

        Assert.Contains(errors, error => error.Contains("unreachable", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WellFormedGraph_ReturnsNoErrors()
    {
        var steps = new[]
        {
            Step("start", WorkflowStepType.Start, "notify"),
            Step("notify", WorkflowStepType.Notification, "end"),
            Step("end", WorkflowStepType.End),
        };

        Assert.True(WorkflowRules.IsValid(steps));
    }

    [Fact]
    public void FindStart_ReturnsTheStartStep()
    {
        var steps = new[] { Step("a", WorkflowStepType.Start), Step("b", WorkflowStepType.End) };

        Assert.Equal("a", WorkflowRules.FindStart(steps)!.Id);
    }

    [Fact]
    public void FindStep_UnknownId_ReturnsNull()
    {
        var steps = new[] { Step("a", WorkflowStepType.Start) };

        Assert.Null(WorkflowRules.FindStep(steps, "missing"));
    }

    [Fact]
    public void GetNextStepId_NonDecisionStep_UsesNextStepIdRegardlessOfBranchResult()
    {
        var step = Step("a", WorkflowStepType.Notification, "b");

        Assert.Equal("b", WorkflowRules.GetNextStepId(step, "true"));
    }

    [Fact]
    public void GetNextStepId_DecisionStep_ResolvesMatchingBranchCaseInsensitively()
    {
        var step = Step("a", WorkflowStepType.Decision, null, new Dictionary<string, string> { ["True"] = "yesBranch", ["False"] = "noBranch" });

        Assert.Equal("yesBranch", WorkflowRules.GetNextStepId(step, "true"));
        Assert.Equal("noBranch", WorkflowRules.GetNextStepId(step, "FALSE"));
    }

    [Fact]
    public void GetNextStepId_DecisionStepWithNoMatchingBranch_FallsBackToNextStepId()
    {
        var step = Step("a", WorkflowStepType.Decision, "fallback", new Dictionary<string, string> { ["true"] = "yesBranch" });

        Assert.Equal("fallback", WorkflowRules.GetNextStepId(step, "unknown"));
    }
}
