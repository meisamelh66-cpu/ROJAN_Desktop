namespace Rojan.Desktop.Domain.Automation;

/// <summary>
/// Pure next-run-time arithmetic for a <see cref="ScheduledJob"/> - no I/O,
/// no timers (that's <c>Infrastructure.Automation.WorkflowSchedulerService</c>'s
/// job). <see cref="ScheduleFrequency.Cron"/> is architecture-ready only:
/// <see cref="ComputeNextRun"/> falls back to a 1-day step for it rather
/// than parsing the expression - a real cron parser is a documented future
/// integration point (Requirement 32.4's "Cron-ready architecture" scope),
/// not built in this phase, the same "contract now, implementation later"
/// boundary <c>Application.Automation.IAiActionExecutor</c> draws for AI.
/// </summary>
public static class ScheduleRules
{
    public static DateTimeOffset ComputeNextRun(ScheduleFrequency frequency, DateTimeOffset from) => frequency switch
    {
        ScheduleFrequency.Hourly => from.AddHours(1),
        ScheduleFrequency.Daily => from.AddDays(1),
        ScheduleFrequency.Weekly => from.AddDays(7),
        ScheduleFrequency.Monthly => from.AddMonths(1),
        ScheduleFrequency.Cron => from.AddDays(1),
        _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, null),
    };

    public static bool IsDue(ScheduledJob job, DateTimeOffset now) => job.IsEnabled && job.NextRunAt <= now;
}
