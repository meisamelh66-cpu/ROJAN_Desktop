using Rojan.Desktop.Application.Notifications;
using AppReporting = Rojan.Desktop.Application.Reporting;
using DomainAutomation = Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Application.Automation;

public sealed class StartStepExecutor : IWorkflowStepExecutor
{
    public WorkflowStepType StepType => WorkflowStepType.Start;

    public Task<StepExecutionResult> ExecuteAsync(WorkflowStepDto workflowStep, AutomationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(StepExecutionResult.Success());
}

public sealed class EndStepExecutor : IWorkflowStepExecutor
{
    public WorkflowStepType StepType => WorkflowStepType.End;

    public Task<StepExecutionResult> ExecuteAsync(WorkflowStepDto workflowStep, AutomationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(StepExecutionResult.Success());
}

/// <summary>Delays by <c>Config["seconds"]</c>, capped at <see cref="MaxDelayMilliseconds"/> so a misconfigured/huge delay can never hang the engine.</summary>
public sealed class DelayStepExecutor : IWorkflowStepExecutor
{
    private const int MaxDelayMilliseconds = 5000;

    public WorkflowStepType StepType => WorkflowStepType.Delay;

    public async Task<StepExecutionResult> ExecuteAsync(WorkflowStepDto workflowStep, AutomationExecutionContext context, CancellationToken cancellationToken = default)
    {
        var seconds = workflowStep.Config.TryGetValue("seconds", out var raw) && int.TryParse(raw, out var parsed) ? parsed : 0;
        var delayMs = Math.Clamp(seconds * 1000, 0, MaxDelayMilliseconds);
        if (delayMs > 0)
        {
            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
        }

        return StepExecutionResult.Success();
    }
}

/// <summary>Evaluates <c>Config["field"]</c>/<c>["operator"]</c>/<c>["value"]</c> (the same shape a <see cref="BusinessRuleConditionDto"/> uses) against the execution's fact bag, reusing <c>Domain.Automation.BusinessRuleEngine</c> directly - Application is allowed to depend on Domain.</summary>
public sealed class DecisionStepExecutor : IWorkflowStepExecutor
{
    public WorkflowStepType StepType => WorkflowStepType.Decision;

    public Task<StepExecutionResult> ExecuteAsync(WorkflowStepDto workflowStep, AutomationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(StepExecutionResult.Success(EvaluateStepCondition(workflowStep, context.Facts) ? "true" : "false"));

    internal static bool EvaluateStepCondition(WorkflowStepDto workflowStep, IReadOnlyDictionary<string, string> facts)
    {
        if (!workflowStep.Config.TryGetValue("field", out var field) ||
            !workflowStep.Config.TryGetValue("operator", out var operatorText) ||
            !workflowStep.Config.TryGetValue("value", out var value) ||
            !Enum.TryParse<DomainAutomation.BusinessRuleOperator>(operatorText, ignoreCase: true, out var op))
        {
            return false;
        }

        return DomainAutomation.BusinessRuleEngine.EvaluateCondition(new DomainAutomation.BusinessRuleCondition(field, op, value), facts);
    }
}

/// <summary>A gate, not a branch: same condition shape as <see cref="DecisionStepExecutor"/>, but a false result stops the whole workflow (successfully, without following any further workflowStep) instead of choosing between two paths.</summary>
public sealed class ConditionStepExecutor : IWorkflowStepExecutor
{
    public WorkflowStepType StepType => WorkflowStepType.Condition;

    public Task<StepExecutionResult> ExecuteAsync(WorkflowStepDto workflowStep, AutomationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(DecisionStepExecutor.EvaluateStepCondition(workflowStep, context.Facts) ? StepExecutionResult.Success() : StepExecutionResult.Stop());
}

/// <summary>Raises a new <c>ApprovalRequest</c> (single workflowStep, approver role from <c>Config["approverRole"]</c>, defaulting to "BranchManager") linked to this execution via <see cref="AutomationExecutionContext.ExecutionId"/>, then pauses the workflow (<see cref="StepExecutionResult.Waiting"/>) - <c>ApprovalService.DecideAsync</c> resumes it once the request is decided.</summary>
public sealed class ApprovalStepExecutor : IWorkflowStepExecutor
{
    private readonly DomainAutomation.IApprovalRepository _approvalRepository;

    public ApprovalStepExecutor(DomainAutomation.IApprovalRepository approvalRepository)
    {
        _approvalRepository = approvalRepository;
    }

    public WorkflowStepType StepType => WorkflowStepType.Approval;

    public async Task<StepExecutionResult> ExecuteAsync(WorkflowStepDto workflowStep, AutomationExecutionContext context, CancellationToken cancellationToken = default)
    {
        var approverRole = workflowStep.Config.TryGetValue("approverRole", out var role) ? role : "BranchManager";
        var approvalType = workflowStep.Config.TryGetValue("approvalType", out var typeText) && Enum.TryParse<DomainAutomation.ApprovalType>(typeText, ignoreCase: true, out var parsedType)
            ? parsedType
            : DomainAutomation.ApprovalType.Branch;

        var request = new DomainAutomation.ApprovalRequest(
            Guid.NewGuid().ToString("N"),
            approvalType,
            workflowStep.Name,
            $"Requested by workflow workflowStep '{workflowStep.Name}'.",
            context.TriggeredByUserId,
            DateTimeOffset.UtcNow,
            [new DomainAutomation.ApprovalStep(0, approverRole, DomainAutomation.ApprovalStepStatus.Pending, null, null, null)],
            DomainAutomation.ApprovalStatus.Pending,
            0,
            context.ExecutionId,
            context.OrganizationId,
            context.BranchId);

        await _approvalRepository.SaveAsync(request, cancellationToken).ConfigureAwait(false);
        return StepExecutionResult.Waiting();
    }
}

/// <summary>Raises a desktop notification via the existing Phase 27 <see cref="INotificationService"/> - Requirement 32.6's "Desktop Notifications"/"Internal Messages" integration, calling into (not modifying) the existing subsystem.</summary>
public sealed class NotificationStepExecutor : IWorkflowStepExecutor
{
    private readonly INotificationService _notificationService;

    public NotificationStepExecutor(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public WorkflowStepType StepType => WorkflowStepType.Notification;

    public async Task<StepExecutionResult> ExecuteAsync(WorkflowStepDto workflowStep, AutomationExecutionContext context, CancellationToken cancellationToken = default)
    {
        var messageKey = workflowStep.Config.TryGetValue("messageKey", out var key) ? key : "Automation_Notification_DefaultMessage";
        await _notificationService.RaiseAsync(
            new NotificationRequest(NotificationSeverity.Information, NotificationPriority.Normal, "Automation_Notification_Title", messageKey, Category: "automation"),
            cancellationToken).ConfigureAwait(false);
        return StepExecutionResult.Success();
    }
}

public sealed class EmailStepExecutor : IWorkflowStepExecutor
{
    private readonly IEmailNotificationService _emailService;

    public EmailStepExecutor(IEmailNotificationService emailService)
    {
        _emailService = emailService;
    }

    public WorkflowStepType StepType => WorkflowStepType.Email;

    public async Task<StepExecutionResult> ExecuteAsync(WorkflowStepDto workflowStep, AutomationExecutionContext context, CancellationToken cancellationToken = default)
    {
        var to = workflowStep.Config.TryGetValue("to", out var toAddress) ? toAddress : string.Empty;
        var subject = workflowStep.Config.TryGetValue("subject", out var subjectText) ? subjectText : workflowStep.Name;
        var body = workflowStep.Config.TryGetValue("body", out var bodyText) ? bodyText : string.Empty;
        await _emailService.SendAsync(new EmailMessage(to, subject, body), cancellationToken).ConfigureAwait(false);
        return StepExecutionResult.Success();
    }
}

public sealed class AiActionStepExecutor : IWorkflowStepExecutor
{
    private readonly IAiActionExecutor _aiActionExecutor;

    public AiActionStepExecutor(IAiActionExecutor aiActionExecutor)
    {
        _aiActionExecutor = aiActionExecutor;
    }

    public WorkflowStepType StepType => WorkflowStepType.AiAction;

    public async Task<StepExecutionResult> ExecuteAsync(WorkflowStepDto workflowStep, AutomationExecutionContext context, CancellationToken cancellationToken = default)
    {
        var result = await _aiActionExecutor.ExecuteAsync(new AiActionRequest(workflowStep.Name, workflowStep.Config), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? StepExecutionResult.Success() : StepExecutionResult.Failure(result.ErrorMessage ?? "AI action failed.");
    }
}

/// <summary>Architecture/contract-only - Requirement 32.7's "no external calls yet" boundary extended to Database steps too: executing arbitrary configured SQL against a real database is a genuine integration this phase deliberately does not build (a security/scope boundary, not an oversight). Always succeeds without doing anything.</summary>
public sealed class DatabaseActionStepExecutor : IWorkflowStepExecutor
{
    public WorkflowStepType StepType => WorkflowStepType.DatabaseAction;

    public Task<StepExecutionResult> ExecuteAsync(WorkflowStepDto workflowStep, AutomationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(StepExecutionResult.Success());
}

/// <summary>Architecture/contract-only - see <see cref="DatabaseActionStepExecutor"/>'s doc comment; the same boundary applied to arbitrary configured HTTP calls.</summary>
public sealed class ApiActionStepExecutor : IWorkflowStepExecutor
{
    public WorkflowStepType StepType => WorkflowStepType.ApiAction;

    public Task<StepExecutionResult> ExecuteAsync(WorkflowStepDto workflowStep, AutomationExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(StepExecutionResult.Success());
}

/// <summary>
/// Phase 33's bridge from the Automation Engine into the Reporting
/// Center - Requirement 33.12's "Integrate with Phase 32 Automation
/// Engine." Runs <c>Config["reportDefinitionId"]</c> (no filters - a
/// scheduled report always runs its full, unfiltered form) and always
/// exports a CSV copy so a scheduled run leaves a real artifact behind,
/// not just an in-memory result the caller immediately discards.
/// <c>Config["recipientEmail"]</c> is optional - when set, the exported
/// file's location is emailed via the existing outbox
/// (<see cref="IEmailNotificationService"/>), the same "architecture
/// ready for delivery, no real SMTP yet" boundary Requirement 32.6
/// already established - so this satisfies 33.12's own "architecture
/// ready for Email delivery" wording exactly.
/// </summary>
public sealed class RunReportStepExecutor : IWorkflowStepExecutor
{
    private readonly AppReporting.IReportExecutionQueryService _executionQueryService;
    private readonly AppReporting.IReportExportService _exportService;
    private readonly IEmailNotificationService _emailService;

    public RunReportStepExecutor(
        AppReporting.IReportExecutionQueryService executionQueryService,
        AppReporting.IReportExportService exportService,
        IEmailNotificationService emailService)
    {
        _executionQueryService = executionQueryService;
        _exportService = exportService;
        _emailService = emailService;
    }

    public WorkflowStepType StepType => WorkflowStepType.RunReport;

    public async Task<StepExecutionResult> ExecuteAsync(WorkflowStepDto workflowStep, AutomationExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (!workflowStep.Config.TryGetValue("reportDefinitionId", out var reportDefinitionId) || string.IsNullOrWhiteSpace(reportDefinitionId))
        {
            return StepExecutionResult.Failure("RunReport step is missing its 'reportDefinitionId' configuration value.");
        }

        AppReporting.ReportResultDto result;
        try
        {
            result = await _executionQueryService.RunReportAsync(reportDefinitionId, [], cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException exception)
        {
            return StepExecutionResult.Failure(exception.Message);
        }

        var exportResult = await _exportService.ExportAsync(result, AppReporting.ExportFormat.Csv, cancellationToken).ConfigureAwait(false);

        if (workflowStep.Config.TryGetValue("recipientEmail", out var recipientEmail) && !string.IsNullOrWhiteSpace(recipientEmail))
        {
            var body = exportResult.Success
                ? $"گزارش «{result.ReportName}» با {result.Rows.Count} ردیف تولید شد. فایل: {exportResult.FilePath}"
                : $"گزارش «{result.ReportName}» با {result.Rows.Count} ردیف تولید شد. خروجی‌گیری ناموفق بود: {exportResult.Message}";
            await _emailService.SendAsync(new EmailMessage(recipientEmail, $"گزارش زمان‌بندی‌شده: {result.ReportName}", body), cancellationToken).ConfigureAwait(false);
        }

        return StepExecutionResult.Success();
    }
}
