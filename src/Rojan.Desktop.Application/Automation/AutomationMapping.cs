using DomainAutomation = Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Application.Automation;

/// <summary>Domain-to-Application (and back) mapping for the Automation vertical slice, shared by every service in this namespace so the translation lives in exactly one place - the same role <c>Workspaces.WorkspaceMapping</c> plays for Phase 29. Every enum pair is mapped via an explicit <see langword="switch"/> (never a raw cast) so a future reordering of either enum can't silently mismatch.</summary>
internal static class AutomationMapping
{
    // ---- Workflow ----

    public static WorkflowStepDto Map(DomainAutomation.WorkflowStep step) =>
        new(step.Id, Map(step.Type), step.Name, step.Config, step.NextStepId, step.Branches);

    public static DomainAutomation.WorkflowStep MapToDomain(WorkflowStepDto step) =>
        new(step.Id, MapToDomain(step.Type), step.Name, step.Config, step.NextStepId, step.Branches);

    public static WorkflowDefinitionDto Map(DomainAutomation.WorkflowDefinition workflow) => new(
        workflow.Id, workflow.ParentWorkflowId, workflow.Name, workflow.Description,
        workflow.Steps.Select(Map).ToList(), workflow.TriggerTypes.Select(Map).ToList(),
        Map(workflow.Status), workflow.Version, workflow.IsEnabled, workflow.CreatedAt, workflow.UpdatedAt,
        workflow.CreatedByUserId, workflow.OrganizationId, workflow.BranchId);

    public static DomainAutomation.WorkflowDefinition MapToDomain(WorkflowDefinitionDto workflow) => new(
        workflow.Id, workflow.ParentWorkflowId, workflow.Name, workflow.Description,
        workflow.Steps.Select(MapToDomain).ToList(), workflow.TriggerTypes.Select(MapToDomain).ToList(),
        MapToDomain(workflow.Status), workflow.Version, workflow.IsEnabled, workflow.CreatedAt, workflow.UpdatedAt,
        workflow.CreatedByUserId, workflow.OrganizationId, workflow.BranchId);

    public static WorkflowStepType Map(DomainAutomation.WorkflowStepType type) => type switch
    {
        DomainAutomation.WorkflowStepType.Start => WorkflowStepType.Start,
        DomainAutomation.WorkflowStepType.End => WorkflowStepType.End,
        DomainAutomation.WorkflowStepType.Decision => WorkflowStepType.Decision,
        DomainAutomation.WorkflowStepType.Delay => WorkflowStepType.Delay,
        DomainAutomation.WorkflowStepType.Approval => WorkflowStepType.Approval,
        DomainAutomation.WorkflowStepType.Condition => WorkflowStepType.Condition,
        DomainAutomation.WorkflowStepType.Notification => WorkflowStepType.Notification,
        DomainAutomation.WorkflowStepType.Email => WorkflowStepType.Email,
        DomainAutomation.WorkflowStepType.AiAction => WorkflowStepType.AiAction,
        DomainAutomation.WorkflowStepType.DatabaseAction => WorkflowStepType.DatabaseAction,
        DomainAutomation.WorkflowStepType.ApiAction => WorkflowStepType.ApiAction,
        DomainAutomation.WorkflowStepType.RunReport => WorkflowStepType.RunReport,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static DomainAutomation.WorkflowStepType MapToDomain(WorkflowStepType type) => type switch
    {
        WorkflowStepType.Start => DomainAutomation.WorkflowStepType.Start,
        WorkflowStepType.End => DomainAutomation.WorkflowStepType.End,
        WorkflowStepType.Decision => DomainAutomation.WorkflowStepType.Decision,
        WorkflowStepType.Delay => DomainAutomation.WorkflowStepType.Delay,
        WorkflowStepType.Approval => DomainAutomation.WorkflowStepType.Approval,
        WorkflowStepType.Condition => DomainAutomation.WorkflowStepType.Condition,
        WorkflowStepType.Notification => DomainAutomation.WorkflowStepType.Notification,
        WorkflowStepType.Email => DomainAutomation.WorkflowStepType.Email,
        WorkflowStepType.AiAction => DomainAutomation.WorkflowStepType.AiAction,
        WorkflowStepType.DatabaseAction => DomainAutomation.WorkflowStepType.DatabaseAction,
        WorkflowStepType.ApiAction => DomainAutomation.WorkflowStepType.ApiAction,
        WorkflowStepType.RunReport => DomainAutomation.WorkflowStepType.RunReport,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static WorkflowStatus Map(DomainAutomation.WorkflowStatus status) => status switch
    {
        DomainAutomation.WorkflowStatus.Draft => WorkflowStatus.Draft,
        DomainAutomation.WorkflowStatus.Published => WorkflowStatus.Published,
        DomainAutomation.WorkflowStatus.Archived => WorkflowStatus.Archived,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static DomainAutomation.WorkflowStatus MapToDomain(WorkflowStatus status) => status switch
    {
        WorkflowStatus.Draft => DomainAutomation.WorkflowStatus.Draft,
        WorkflowStatus.Published => DomainAutomation.WorkflowStatus.Published,
        WorkflowStatus.Archived => DomainAutomation.WorkflowStatus.Archived,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static TriggerType Map(DomainAutomation.TriggerType trigger) => trigger switch
    {
        DomainAutomation.TriggerType.AppointmentCreated => TriggerType.AppointmentCreated,
        DomainAutomation.TriggerType.AppointmentCancelled => TriggerType.AppointmentCancelled,
        DomainAutomation.TriggerType.CustomerRegistered => TriggerType.CustomerRegistered,
        DomainAutomation.TriggerType.PaymentCompleted => TriggerType.PaymentCompleted,
        DomainAutomation.TriggerType.LowInventory => TriggerType.LowInventory,
        DomainAutomation.TriggerType.EmployeeCreated => TriggerType.EmployeeCreated,
        DomainAutomation.TriggerType.BranchCreated => TriggerType.BranchCreated,
        DomainAutomation.TriggerType.LicenseExpired => TriggerType.LicenseExpired,
        DomainAutomation.TriggerType.Login => TriggerType.Login,
        DomainAutomation.TriggerType.Logout => TriggerType.Logout,
        _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger, null),
    };

    public static DomainAutomation.TriggerType MapToDomain(TriggerType trigger) => trigger switch
    {
        TriggerType.AppointmentCreated => DomainAutomation.TriggerType.AppointmentCreated,
        TriggerType.AppointmentCancelled => DomainAutomation.TriggerType.AppointmentCancelled,
        TriggerType.CustomerRegistered => DomainAutomation.TriggerType.CustomerRegistered,
        TriggerType.PaymentCompleted => DomainAutomation.TriggerType.PaymentCompleted,
        TriggerType.LowInventory => DomainAutomation.TriggerType.LowInventory,
        TriggerType.EmployeeCreated => DomainAutomation.TriggerType.EmployeeCreated,
        TriggerType.BranchCreated => DomainAutomation.TriggerType.BranchCreated,
        TriggerType.LicenseExpired => DomainAutomation.TriggerType.LicenseExpired,
        TriggerType.Login => DomainAutomation.TriggerType.Login,
        TriggerType.Logout => DomainAutomation.TriggerType.Logout,
        _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger, null),
    };

    // ---- Execution ----

    public static WorkflowExecutionStatus Map(DomainAutomation.WorkflowExecutionStatus status) => status switch
    {
        DomainAutomation.WorkflowExecutionStatus.Running => WorkflowExecutionStatus.Running,
        DomainAutomation.WorkflowExecutionStatus.Waiting => WorkflowExecutionStatus.Waiting,
        DomainAutomation.WorkflowExecutionStatus.Completed => WorkflowExecutionStatus.Completed,
        DomainAutomation.WorkflowExecutionStatus.Failed => WorkflowExecutionStatus.Failed,
        DomainAutomation.WorkflowExecutionStatus.Cancelled => WorkflowExecutionStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    private static DomainAutomation.WorkflowExecutionStatus MapToDomain(WorkflowExecutionStatus status) => status switch
    {
        WorkflowExecutionStatus.Running => DomainAutomation.WorkflowExecutionStatus.Running,
        WorkflowExecutionStatus.Waiting => DomainAutomation.WorkflowExecutionStatus.Waiting,
        WorkflowExecutionStatus.Completed => DomainAutomation.WorkflowExecutionStatus.Completed,
        WorkflowExecutionStatus.Failed => DomainAutomation.WorkflowExecutionStatus.Failed,
        WorkflowExecutionStatus.Cancelled => DomainAutomation.WorkflowExecutionStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static StepExecutionStatus Map(DomainAutomation.StepExecutionStatus status) => status switch
    {
        DomainAutomation.StepExecutionStatus.Pending => StepExecutionStatus.Pending,
        DomainAutomation.StepExecutionStatus.Running => StepExecutionStatus.Running,
        DomainAutomation.StepExecutionStatus.Completed => StepExecutionStatus.Completed,
        DomainAutomation.StepExecutionStatus.Failed => StepExecutionStatus.Failed,
        DomainAutomation.StepExecutionStatus.Skipped => StepExecutionStatus.Skipped,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static DomainAutomation.StepExecutionStatus MapToDomain(StepExecutionStatus status) => status switch
    {
        StepExecutionStatus.Pending => DomainAutomation.StepExecutionStatus.Pending,
        StepExecutionStatus.Running => DomainAutomation.StepExecutionStatus.Running,
        StepExecutionStatus.Completed => DomainAutomation.StepExecutionStatus.Completed,
        StepExecutionStatus.Failed => DomainAutomation.StepExecutionStatus.Failed,
        StepExecutionStatus.Skipped => DomainAutomation.StepExecutionStatus.Skipped,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static WorkflowStepExecutionLogDto Map(DomainAutomation.WorkflowStepExecutionLog log) =>
        new(log.StepId, log.StepName, Map(log.StepType), Map(log.Status), log.StartedAt, log.CompletedAt, log.AttemptCount, log.ErrorMessage);

    public static DomainAutomation.WorkflowStepExecutionLog MapToDomain(WorkflowStepExecutionLogDto log) =>
        new(log.StepId, log.StepName, MapToDomain(log.StepType), MapToDomain(log.Status), log.StartedAt, log.CompletedAt, log.AttemptCount, log.ErrorMessage);

    public static WorkflowExecutionDto Map(DomainAutomation.WorkflowExecution execution) => new(
        execution.Id, execution.WorkflowId, execution.WorkflowVersion, execution.WorkflowName,
        Map(execution.Status), execution.TriggeredByTrigger is { } trigger ? Map(trigger) : null,
        execution.TriggeredByUserId, execution.StepLogs.Select(Map).ToList(), execution.StartedAt,
        execution.CompletedAt, execution.DurationMs, execution.ErrorMessage, execution.OrganizationId, execution.BranchId);

    public static DomainAutomation.WorkflowExecution MapToDomain(WorkflowExecutionDto execution) => new(
        execution.Id, execution.WorkflowId, execution.WorkflowVersion, execution.WorkflowName,
        MapToDomain(execution.Status), execution.TriggeredByTrigger is { } trigger ? MapToDomain(trigger) : null,
        execution.TriggeredByUserId, execution.StepLogs.Select(MapToDomain).ToList(), execution.StartedAt,
        execution.CompletedAt, execution.DurationMs, execution.ErrorMessage, execution.OrganizationId, execution.BranchId);

    // ---- Business Rules ----

    public static BusinessRuleOperator Map(DomainAutomation.BusinessRuleOperator op) => op switch
    {
        DomainAutomation.BusinessRuleOperator.Equals => BusinessRuleOperator.Equals,
        DomainAutomation.BusinessRuleOperator.NotEquals => BusinessRuleOperator.NotEquals,
        DomainAutomation.BusinessRuleOperator.GreaterThan => BusinessRuleOperator.GreaterThan,
        DomainAutomation.BusinessRuleOperator.GreaterThanOrEqual => BusinessRuleOperator.GreaterThanOrEqual,
        DomainAutomation.BusinessRuleOperator.LessThan => BusinessRuleOperator.LessThan,
        DomainAutomation.BusinessRuleOperator.LessThanOrEqual => BusinessRuleOperator.LessThanOrEqual,
        DomainAutomation.BusinessRuleOperator.Contains => BusinessRuleOperator.Contains,
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, null),
    };

    public static DomainAutomation.BusinessRuleOperator MapToDomain(BusinessRuleOperator op) => op switch
    {
        BusinessRuleOperator.Equals => DomainAutomation.BusinessRuleOperator.Equals,
        BusinessRuleOperator.NotEquals => DomainAutomation.BusinessRuleOperator.NotEquals,
        BusinessRuleOperator.GreaterThan => DomainAutomation.BusinessRuleOperator.GreaterThan,
        BusinessRuleOperator.GreaterThanOrEqual => DomainAutomation.BusinessRuleOperator.GreaterThanOrEqual,
        BusinessRuleOperator.LessThan => DomainAutomation.BusinessRuleOperator.LessThan,
        BusinessRuleOperator.LessThanOrEqual => DomainAutomation.BusinessRuleOperator.LessThanOrEqual,
        BusinessRuleOperator.Contains => DomainAutomation.BusinessRuleOperator.Contains,
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, null),
    };

    public static BusinessRuleActionType Map(DomainAutomation.BusinessRuleActionType type) => type switch
    {
        DomainAutomation.BusinessRuleActionType.RaiseNotification => BusinessRuleActionType.RaiseNotification,
        DomainAutomation.BusinessRuleActionType.ApplyDiscount => BusinessRuleActionType.ApplyDiscount,
        DomainAutomation.BusinessRuleActionType.NotifyManager => BusinessRuleActionType.NotifyManager,
        DomainAutomation.BusinessRuleActionType.TriggerWorkflow => BusinessRuleActionType.TriggerWorkflow,
        DomainAutomation.BusinessRuleActionType.Custom => BusinessRuleActionType.Custom,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static DomainAutomation.BusinessRuleActionType MapToDomain(BusinessRuleActionType type) => type switch
    {
        BusinessRuleActionType.RaiseNotification => DomainAutomation.BusinessRuleActionType.RaiseNotification,
        BusinessRuleActionType.ApplyDiscount => DomainAutomation.BusinessRuleActionType.ApplyDiscount,
        BusinessRuleActionType.NotifyManager => DomainAutomation.BusinessRuleActionType.NotifyManager,
        BusinessRuleActionType.TriggerWorkflow => DomainAutomation.BusinessRuleActionType.TriggerWorkflow,
        BusinessRuleActionType.Custom => DomainAutomation.BusinessRuleActionType.Custom,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static BusinessRuleConditionDto Map(DomainAutomation.BusinessRuleCondition condition) =>
        new(condition.Field, Map(condition.Operator), condition.Value);

    public static DomainAutomation.BusinessRuleCondition MapToDomain(BusinessRuleConditionDto condition) =>
        new(condition.Field, MapToDomain(condition.Operator), condition.Value);

    public static BusinessRuleActionDto Map(DomainAutomation.BusinessRuleAction action) =>
        new(Map(action.Type), action.Parameters);

    public static DomainAutomation.BusinessRuleAction MapToDomain(BusinessRuleActionDto action) =>
        new(MapToDomain(action.Type), action.Parameters);

    public static BusinessRuleDto Map(DomainAutomation.BusinessRule rule) => new(
        rule.Id, rule.Name, rule.Description, rule.Conditions.Select(Map).ToList(), Map(rule.Action),
        rule.Priority, rule.IsEnabled, rule.CreatedAt, rule.UpdatedAt, rule.OrganizationId, rule.BranchId);

    public static DomainAutomation.BusinessRule MapToDomain(BusinessRuleDto rule) => new(
        rule.Id, rule.Name, rule.Description, rule.Conditions.Select(MapToDomain).ToList(), MapToDomain(rule.Action),
        rule.Priority, rule.IsEnabled, rule.CreatedAt, rule.UpdatedAt, rule.OrganizationId, rule.BranchId);

    // ---- Scheduled Jobs ----

    public static ScheduleFrequency Map(DomainAutomation.ScheduleFrequency frequency) => frequency switch
    {
        DomainAutomation.ScheduleFrequency.Hourly => ScheduleFrequency.Hourly,
        DomainAutomation.ScheduleFrequency.Daily => ScheduleFrequency.Daily,
        DomainAutomation.ScheduleFrequency.Weekly => ScheduleFrequency.Weekly,
        DomainAutomation.ScheduleFrequency.Monthly => ScheduleFrequency.Monthly,
        DomainAutomation.ScheduleFrequency.Cron => ScheduleFrequency.Cron,
        _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, null),
    };

    public static DomainAutomation.ScheduleFrequency MapToDomain(ScheduleFrequency frequency) => frequency switch
    {
        ScheduleFrequency.Hourly => DomainAutomation.ScheduleFrequency.Hourly,
        ScheduleFrequency.Daily => DomainAutomation.ScheduleFrequency.Daily,
        ScheduleFrequency.Weekly => DomainAutomation.ScheduleFrequency.Weekly,
        ScheduleFrequency.Monthly => DomainAutomation.ScheduleFrequency.Monthly,
        ScheduleFrequency.Cron => DomainAutomation.ScheduleFrequency.Cron,
        _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, null),
    };

    public static ScheduledJobDto Map(DomainAutomation.ScheduledJob job) => new(
        job.Id, job.Name, Map(job.Frequency), job.CronExpression, job.WorkflowId, job.IsEnabled,
        job.NextRunAt, job.LastRunAt, job.OrganizationId, job.BranchId);

    public static DomainAutomation.ScheduledJob MapToDomain(ScheduledJobDto job) => new(
        job.Id, job.Name, MapToDomain(job.Frequency), job.CronExpression, job.WorkflowId, job.IsEnabled,
        job.NextRunAt, job.LastRunAt, job.OrganizationId, job.BranchId);

    // ---- Approvals ----

    public static ApprovalType Map(DomainAutomation.ApprovalType type) => type switch
    {
        DomainAutomation.ApprovalType.Leave => ApprovalType.Leave,
        DomainAutomation.ApprovalType.Expense => ApprovalType.Expense,
        DomainAutomation.ApprovalType.Inventory => ApprovalType.Inventory,
        DomainAutomation.ApprovalType.Branch => ApprovalType.Branch,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static DomainAutomation.ApprovalType MapToDomain(ApprovalType type) => type switch
    {
        ApprovalType.Leave => DomainAutomation.ApprovalType.Leave,
        ApprovalType.Expense => DomainAutomation.ApprovalType.Expense,
        ApprovalType.Inventory => DomainAutomation.ApprovalType.Inventory,
        ApprovalType.Branch => DomainAutomation.ApprovalType.Branch,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    public static ApprovalStatus Map(DomainAutomation.ApprovalStatus status) => status switch
    {
        DomainAutomation.ApprovalStatus.Pending => ApprovalStatus.Pending,
        DomainAutomation.ApprovalStatus.Approved => ApprovalStatus.Approved,
        DomainAutomation.ApprovalStatus.Rejected => ApprovalStatus.Rejected,
        DomainAutomation.ApprovalStatus.Cancelled => ApprovalStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    private static DomainAutomation.ApprovalStatus MapToDomain(ApprovalStatus status) => status switch
    {
        ApprovalStatus.Pending => DomainAutomation.ApprovalStatus.Pending,
        ApprovalStatus.Approved => DomainAutomation.ApprovalStatus.Approved,
        ApprovalStatus.Rejected => DomainAutomation.ApprovalStatus.Rejected,
        ApprovalStatus.Cancelled => DomainAutomation.ApprovalStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static ApprovalStepStatus Map(DomainAutomation.ApprovalStepStatus status) => status switch
    {
        DomainAutomation.ApprovalStepStatus.Pending => ApprovalStepStatus.Pending,
        DomainAutomation.ApprovalStepStatus.Approved => ApprovalStepStatus.Approved,
        DomainAutomation.ApprovalStepStatus.Rejected => ApprovalStepStatus.Rejected,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static DomainAutomation.ApprovalStepStatus MapToDomain(ApprovalStepStatus status) => status switch
    {
        ApprovalStepStatus.Pending => DomainAutomation.ApprovalStepStatus.Pending,
        ApprovalStepStatus.Approved => DomainAutomation.ApprovalStepStatus.Approved,
        ApprovalStepStatus.Rejected => DomainAutomation.ApprovalStepStatus.Rejected,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static ApprovalStepDto Map(DomainAutomation.ApprovalStep step) =>
        new(step.StepIndex, step.ApproverRole, Map(step.Status), step.DecidedByUserId, step.DecidedAt, step.Comment);

    public static DomainAutomation.ApprovalStep MapToDomain(ApprovalStepDto step) =>
        new(step.StepIndex, step.ApproverRole, MapToDomain(step.Status), step.DecidedByUserId, step.DecidedAt, step.Comment);

    public static ApprovalRequestDto Map(DomainAutomation.ApprovalRequest request) => new(
        request.Id, Map(request.Type), request.Title, request.Description, request.RequestedByUserId,
        request.RequestedAt, request.Steps.Select(Map).ToList(), Map(request.Status), request.CurrentStepIndex,
        request.WorkflowExecutionId, request.OrganizationId, request.BranchId);

    public static DomainAutomation.ApprovalRequest MapToDomain(ApprovalRequestDto request) => new(
        request.Id, MapToDomain(request.Type), request.Title, request.Description, request.RequestedByUserId,
        request.RequestedAt, request.Steps.Select(MapToDomain).ToList(), MapToDomain(request.Status), request.CurrentStepIndex,
        request.WorkflowExecutionId, request.OrganizationId, request.BranchId);

    // ---- Retry policy ----

    public static RetryPolicyDto Map(DomainAutomation.RetryPolicy policy) =>
        new(policy.MaxRetries, policy.RetryDelaySeconds, policy.TimeoutSeconds);

    public static DomainAutomation.RetryPolicy MapToDomain(RetryPolicyDto policy) =>
        new(policy.MaxRetries, policy.RetryDelaySeconds, policy.TimeoutSeconds);
}
