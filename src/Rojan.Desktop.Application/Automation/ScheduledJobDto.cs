namespace Rojan.Desktop.Application.Automation;

/// <summary>Application's own mirror of <c>Domain.Automation.ScheduleFrequency</c>.</summary>
public enum ScheduleFrequency
{
    Hourly,
    Daily,
    Weekly,
    Monthly,
    Cron,
}

/// <summary>Application's own mirror of <c>Domain.Automation.ScheduledJob</c>.</summary>
public sealed record ScheduledJobDto(
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
