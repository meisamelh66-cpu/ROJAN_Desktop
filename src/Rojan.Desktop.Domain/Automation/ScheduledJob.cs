namespace Rojan.Desktop.Domain.Automation;

/// <summary>How often a <see cref="ScheduledJob"/> recurs. <see cref="Cron"/> is architecture-ready only in this phase - <see cref="ScheduleRules.ComputeNextRun"/>'s own doc comment explains the boundary.</summary>
public enum ScheduleFrequency
{
    Hourly,
    Daily,
    Weekly,
    Monthly,
    Cron,
}

/// <summary>
/// A recurring trigger for a <see cref="WorkflowDefinition"/> - Requirement
/// 32.4 (Scheduled Jobs), as returned by <see cref="IScheduledJobRepository"/>.
/// <see cref="CronExpression"/> is populated only when
/// <see cref="Frequency"/> is <see cref="ScheduleFrequency.Cron"/>.
/// </summary>
public sealed record ScheduledJob(
    string Id,
    string Name,
    ScheduleFrequency Frequency,
    string? CronExpression,
    string WorkflowId,
    bool IsEnabled,
    DateTimeOffset NextRunAt,
    DateTimeOffset? LastRunAt,
    string OrganizationId,
    string BranchId);
