using DomainAutomation = Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Application.Automation;

/// <summary>
/// Default <see cref="IWorkflowExecutionEngine"/>. Walks
/// <c>Domain.Automation.WorkflowRules</c>' step graph directly (Application
/// is allowed to depend on Domain - only Presentation cannot), dispatching
/// each step to whichever <see cref="IWorkflowStepExecutor"/> registered
/// for its <see cref="WorkflowStepType"/> (built once from
/// <c>IEnumerable&lt;IWorkflowStepExecutor&gt;</c> - one DI registration per
/// step type, see <c>Application.DependencyInjection.AddApplication</c>).
/// </summary>
public sealed class WorkflowExecutionEngine : IWorkflowExecutionEngine
{
    private const int MaxRetryDelaySeconds = 30;

    private readonly DomainAutomation.IWorkflowRepository _workflowRepository;
    private readonly DomainAutomation.IWorkflowExecutionRepository _executionRepository;
    private readonly Dictionary<WorkflowStepType, IWorkflowStepExecutor> _executorsByType;

    public WorkflowExecutionEngine(
        DomainAutomation.IWorkflowRepository workflowRepository,
        DomainAutomation.IWorkflowExecutionRepository executionRepository,
        IEnumerable<IWorkflowStepExecutor> executors)
    {
        _workflowRepository = workflowRepository;
        _executionRepository = executionRepository;
        _executorsByType = executors.ToDictionary(executor => executor.StepType);
    }

    public async Task<WorkflowExecutionDto> ExecuteAsync(
        string workflowId,
        TriggerType? trigger,
        string triggeredByUserId,
        IReadOnlyDictionary<string, string> facts,
        string organizationId,
        string branchId,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowRepository.GetByIdAsync(workflowId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Workflow '{workflowId}' was not found.");

        var start = DomainAutomation.WorkflowRules.FindStart(workflow.Steps)
            ?? throw new InvalidOperationException($"Workflow '{workflowId}' has no Start step.");

        var executionId = Guid.NewGuid().ToString("N");
        var startedAt = DateTimeOffset.UtcNow;
        var context = new AutomationExecutionContext(executionId, facts, organizationId, branchId, triggeredByUserId);

        var (status, stepLogs, errorMessage) = await RunLoopAsync(workflow.Steps, start, [], context, cancellationToken).ConfigureAwait(false);

        var domainTrigger = trigger is { } triggerValue ? AutomationMapping.MapToDomain(triggerValue) : (DomainAutomation.TriggerType?)null;
        var execution = BuildExecution(executionId, workflow, status, domainTrigger, triggeredByUserId, stepLogs, startedAt, organizationId, branchId, errorMessage);
        await _executionRepository.SaveAsync(execution, cancellationToken).ConfigureAwait(false);
        return AutomationMapping.Map(execution);
    }

    public async Task<WorkflowExecutionDto> ResumeApprovalAsync(string executionId, bool approved, CancellationToken cancellationToken = default)
    {
        var execution = await _executionRepository.GetByIdAsync(executionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Execution '{executionId}' was not found.");

        if (execution.Status != DomainAutomation.WorkflowExecutionStatus.Waiting)
        {
            throw new InvalidOperationException($"Execution '{executionId}' is not waiting on an approval.");
        }

        var workflow = await _workflowRepository.GetByIdAsync(execution.WorkflowId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Workflow '{execution.WorkflowId}' was not found.");

        var pausedStepLog = execution.StepLogs[^1];
        var pausedStep = DomainAutomation.WorkflowRules.FindStep(workflow.Steps, pausedStepLog.StepId)
            ?? throw new InvalidOperationException($"Step '{pausedStepLog.StepId}' was not found in workflow '{workflow.Id}'.");

        if (!approved)
        {
            var completedAt = DateTimeOffset.UtcNow;
            var rejectedExecution = execution with
            {
                Status = DomainAutomation.WorkflowExecutionStatus.Failed,
                CompletedAt = completedAt,
                DurationMs = DomainAutomation.WorkflowExecutionRules.ComputeDurationMs(execution.StartedAt, completedAt),
                ErrorMessage = "The approval request for this step was rejected.",
            };
            await _executionRepository.SaveAsync(rejectedExecution, cancellationToken).ConfigureAwait(false);
            return AutomationMapping.Map(rejectedExecution);
        }

        var nextStepId = DomainAutomation.WorkflowRules.GetNextStepId(pausedStep);
        var nextStep = DomainAutomation.WorkflowRules.FindStep(workflow.Steps, nextStepId);
        var context = new AutomationExecutionContext(executionId, new Dictionary<string, string>(), execution.OrganizationId, execution.BranchId, execution.TriggeredByUserId);

        var (status, stepLogs, errorMessage) = await RunLoopAsync(workflow.Steps, nextStep, execution.StepLogs, context, cancellationToken).ConfigureAwait(false);

        var resumedExecution = BuildExecution(
            executionId, workflow, status, execution.TriggeredByTrigger, execution.TriggeredByUserId, stepLogs,
            execution.StartedAt, execution.OrganizationId, execution.BranchId, errorMessage);
        await _executionRepository.SaveAsync(resumedExecution, cancellationToken).ConfigureAwait(false);
        return AutomationMapping.Map(resumedExecution);
    }

    public async Task<IReadOnlyList<WorkflowExecutionDto>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        var executions = await _executionRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return executions.OrderByDescending(execution => execution.StartedAt).Select(AutomationMapping.Map).ToList();
    }

    public async Task<WorkflowExecutionDto?> GetByIdAsync(string executionId, CancellationToken cancellationToken = default)
    {
        var execution = await _executionRepository.GetByIdAsync(executionId, cancellationToken).ConfigureAwait(false);
        return execution is null ? null : AutomationMapping.Map(execution);
    }

    private async Task<(DomainAutomation.WorkflowExecutionStatus Status, List<DomainAutomation.WorkflowStepExecutionLog> StepLogs, string? ErrorMessage)> RunLoopAsync(
        IReadOnlyList<DomainAutomation.WorkflowStep> steps,
        DomainAutomation.WorkflowStep? current,
        IReadOnlyList<DomainAutomation.WorkflowStepExecutionLog> existingLogs,
        AutomationExecutionContext context,
        CancellationToken cancellationToken)
    {
        var stepLogs = existingLogs.ToList();
        string? errorMessage = null;
        var status = DomainAutomation.WorkflowExecutionStatus.Running;

        while (current is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stepStartedAt = DateTimeOffset.UtcNow;

            if (!_executorsByType.TryGetValue(AutomationMapping.Map(current.Type), out var executor))
            {
                errorMessage = $"No executor registered for step type '{current.Type}'.";
                stepLogs.Add(new DomainAutomation.WorkflowStepExecutionLog(current.Id, current.Name, current.Type, DomainAutomation.StepExecutionStatus.Failed, stepStartedAt, DateTimeOffset.UtcNow, 1, errorMessage));
                status = DomainAutomation.WorkflowExecutionStatus.Failed;
                break;
            }

            // Requirement 32.11 (Error Recovery): each step's own retry
            // policy comes from its Config bag ("maxRetries"/
            // "retryDelaySeconds"/"timeoutSeconds" - the same "flat
            // string-keyed config" shape Delay's "seconds" already uses),
            // defaulting to RetryPolicy.None (no retry) so every existing
            // step keeps working unchanged. Backoff delay is capped at
            // MaxRetryDelaySeconds regardless of configuration, so a
            // misconfigured policy can never stall the engine for long.
            var policy = ParseRetryPolicy(current.Config);
            var attempt = 1;
            var stepDto = AutomationMapping.Map(current);
            StepExecutionResult result;
            DateTimeOffset stepCompletedAt;
            while (true)
            {
                result = await executor.ExecuteAsync(stepDto, context, cancellationToken).ConfigureAwait(false);
                stepCompletedAt = DateTimeOffset.UtcNow;

                if (result.IsSuccess || !DomainAutomation.RetryRules.ShouldRetry(attempt, policy))
                {
                    break;
                }

                var delaySeconds = DomainAutomation.RetryRules.ComputeBackoffDelaySeconds(attempt, policy);
                if (delaySeconds > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(delaySeconds, MaxRetryDelaySeconds)), cancellationToken).ConfigureAwait(false);
                }

                attempt++;
            }

            if (!result.IsSuccess)
            {
                errorMessage = result.ErrorMessage;
                stepLogs.Add(new DomainAutomation.WorkflowStepExecutionLog(current.Id, current.Name, current.Type, DomainAutomation.StepExecutionStatus.Failed, stepStartedAt, stepCompletedAt, attempt, errorMessage));
                status = DomainAutomation.WorkflowExecutionStatus.Failed;
                break;
            }

            stepLogs.Add(new DomainAutomation.WorkflowStepExecutionLog(current.Id, current.Name, current.Type, DomainAutomation.StepExecutionStatus.Completed, stepStartedAt, stepCompletedAt, attempt, null));

            if (result.IsWaiting)
            {
                status = DomainAutomation.WorkflowExecutionStatus.Waiting;
                break;
            }

            if (result.StopWorkflow || current.Type == DomainAutomation.WorkflowStepType.End)
            {
                status = DomainAutomation.WorkflowExecutionStatus.Completed;
                break;
            }

            var nextStepId = DomainAutomation.WorkflowRules.GetNextStepId(current, result.BranchResult);
            current = DomainAutomation.WorkflowRules.FindStep(steps, nextStepId);
            if (current is null)
            {
                status = DomainAutomation.WorkflowExecutionStatus.Completed;
            }
        }

        return (status, stepLogs, errorMessage);
    }

    /// <summary>Reads a step's own retry policy from its <c>Config</c> bag, defaulting to <see cref="DomainAutomation.RetryPolicy.None"/> (no retry) so any step that doesn't configure one keeps its original single-attempt behavior.</summary>
    private static DomainAutomation.RetryPolicy ParseRetryPolicy(IReadOnlyDictionary<string, string> config)
    {
        var maxRetries = config.TryGetValue("maxRetries", out var maxRetriesText) && int.TryParse(maxRetriesText, out var parsedMaxRetries)
            ? Math.Max(0, parsedMaxRetries)
            : DomainAutomation.RetryPolicy.None.MaxRetries;

        var retryDelaySeconds = config.TryGetValue("retryDelaySeconds", out var delayText) && int.TryParse(delayText, out var parsedDelay)
            ? Math.Max(0, parsedDelay)
            : DomainAutomation.RetryPolicy.None.RetryDelaySeconds;

        var timeoutSeconds = config.TryGetValue("timeoutSeconds", out var timeoutText) && int.TryParse(timeoutText, out var parsedTimeout) && parsedTimeout > 0
            ? parsedTimeout
            : DomainAutomation.RetryPolicy.None.TimeoutSeconds;

        return new DomainAutomation.RetryPolicy(maxRetries, retryDelaySeconds, timeoutSeconds);
    }

    private static DomainAutomation.WorkflowExecution BuildExecution(
        string executionId,
        DomainAutomation.WorkflowDefinition workflow,
        DomainAutomation.WorkflowExecutionStatus status,
        DomainAutomation.TriggerType? trigger,
        string triggeredByUserId,
        List<DomainAutomation.WorkflowStepExecutionLog> stepLogs,
        DateTimeOffset startedAt,
        string organizationId,
        string branchId,
        string? errorMessage)
    {
        var isTerminal = DomainAutomation.WorkflowExecutionRules.IsTerminal(status);
        var completedAt = isTerminal ? DateTimeOffset.UtcNow : (DateTimeOffset?)null;
        var durationMs = completedAt.HasValue ? DomainAutomation.WorkflowExecutionRules.ComputeDurationMs(startedAt, completedAt.Value) : (long?)null;

        return new DomainAutomation.WorkflowExecution(
            executionId, workflow.Id, workflow.Version, workflow.Name, status, trigger, triggeredByUserId,
            stepLogs, startedAt, completedAt, durationMs, errorMessage, organizationId, branchId);
    }
}
