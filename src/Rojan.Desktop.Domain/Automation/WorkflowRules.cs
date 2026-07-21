namespace Rojan.Desktop.Domain.Automation;

/// <summary>
/// Pure structural validation and graph-traversal logic for a workflow's
/// step list - a deliberate deviation from this codebase's usual "Domain
/// is just data + repository contract" minimalism, same reasoning as
/// <c>Bookings.BookingRules</c>/<c>Workspaces.WorkspaceRules</c>: a
/// workflow that references a non-existent next-step id, has no
/// <see cref="WorkflowStepType.Start"/>, or can never reach
/// <see cref="WorkflowStepType.End"/> has to be caught somewhere both
/// Application and Presentation can trust without re-deriving it.
/// </summary>
public static class WorkflowRules
{
    /// <summary>Every structural problem with <paramref name="steps"/> - empty means the graph is valid. Never throws; a caller (e.g. a future designer) can show every problem at once rather than one exception at a time.</summary>
    public static IReadOnlyList<string> Validate(IReadOnlyList<WorkflowStep> steps)
    {
        var errors = new List<string>();
        if (steps.Count == 0)
        {
            errors.Add("A workflow must have at least one step.");
            return errors;
        }

        var startSteps = steps.Where(s => s.Type == WorkflowStepType.Start).ToList();
        if (startSteps.Count != 1)
        {
            errors.Add($"A workflow must have exactly one Start step (found {startSteps.Count}).");
        }

        if (steps.All(s => s.Type != WorkflowStepType.End))
        {
            errors.Add("A workflow must have at least one End step.");
        }

        var idsById = steps.Select(s => s.Id).ToHashSet();
        foreach (var step in steps)
        {
            foreach (var targetId in TargetStepIds(step))
            {
                if (!idsById.Contains(targetId))
                {
                    errors.Add($"Step '{step.Name}' references a next step id '{targetId}' that does not exist.");
                }
            }
        }

        if (startSteps.Count == 1)
        {
            var reachable = ReachableStepIds(steps, startSteps[0].Id);
            var unreachable = steps.Where(s => s.Type != WorkflowStepType.Start && !reachable.Contains(s.Id)).ToList();
            foreach (var step in unreachable)
            {
                errors.Add($"Step '{step.Name}' is unreachable from Start.");
            }
        }

        return errors;
    }

    public static bool IsValid(IReadOnlyList<WorkflowStep> steps) => Validate(steps).Count == 0;

    public static WorkflowStep? FindStep(IReadOnlyList<WorkflowStep> steps, string? stepId) =>
        stepId is null ? null : steps.FirstOrDefault(s => s.Id == stepId);

    public static WorkflowStep? FindStart(IReadOnlyList<WorkflowStep> steps) =>
        steps.FirstOrDefault(s => s.Type == WorkflowStepType.Start);

    /// <summary>The next step to run after <paramref name="step"/>. For <see cref="WorkflowStepType.Decision"/>, resolves via <see cref="WorkflowStep.Branches"/> keyed by <paramref name="branchResult"/> (case-insensitive); every other step type uses <see cref="WorkflowStep.NextStepId"/> unconditionally.</summary>
    public static string? GetNextStepId(WorkflowStep step, string? branchResult = null)
    {
        if (step.Type == WorkflowStepType.Decision && step.Branches is not null && branchResult is not null)
        {
            var match = step.Branches.FirstOrDefault(kvp => string.Equals(kvp.Key, branchResult, StringComparison.OrdinalIgnoreCase));
            return match.Key is null ? step.NextStepId : match.Value;
        }

        return step.NextStepId;
    }

    private static IEnumerable<string> TargetStepIds(WorkflowStep step)
    {
        if (step.NextStepId is not null)
        {
            yield return step.NextStepId;
        }

        if (step.Branches is not null)
        {
            foreach (var targetId in step.Branches.Values)
            {
                yield return targetId;
            }
        }
    }

    private static HashSet<string> ReachableStepIds(IReadOnlyList<WorkflowStep> steps, string startId)
    {
        var byId = steps.ToDictionary(s => s.Id);
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(startId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            if (!visited.Add(currentId) || !byId.TryGetValue(currentId, out var current))
            {
                continue;
            }

            foreach (var targetId in TargetStepIds(current))
            {
                if (!visited.Contains(targetId))
                {
                    queue.Enqueue(targetId);
                }
            }
        }

        return visited;
    }
}
