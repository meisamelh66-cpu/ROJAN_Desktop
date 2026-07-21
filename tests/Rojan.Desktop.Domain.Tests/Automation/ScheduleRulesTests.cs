using Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Domain.Tests.Automation;

/// <summary>Exercises <see cref="ScheduleRules"/> - next-run-time arithmetic and due-check.</summary>
public sealed class ScheduleRulesTests
{
    private static readonly DateTimeOffset From = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ScheduleFrequency.Hourly, 0, 1, 0, 0)]
    [InlineData(ScheduleFrequency.Daily, 1, 0, 0, 0)]
    [InlineData(ScheduleFrequency.Weekly, 7, 0, 0, 0)]
    public void ComputeNextRun_AddsTheExpectedInterval(ScheduleFrequency frequency, int days, int hours, int minutes, int seconds)
    {
        var expected = From.AddDays(days).AddHours(hours).AddMinutes(minutes).AddSeconds(seconds);

        Assert.Equal(expected, ScheduleRules.ComputeNextRun(frequency, From));
    }

    [Fact]
    public void ComputeNextRun_Monthly_AddsOneCalendarMonth()
    {
        Assert.Equal(From.AddMonths(1), ScheduleRules.ComputeNextRun(ScheduleFrequency.Monthly, From));
    }

    [Fact]
    public void ComputeNextRun_Cron_FallsBackToOneDay()
    {
        Assert.Equal(From.AddDays(1), ScheduleRules.ComputeNextRun(ScheduleFrequency.Cron, From));
    }

    private static ScheduledJob Job(DateTimeOffset nextRunAt, bool isEnabled = true) =>
        new("job-1", "Job", ScheduleFrequency.Daily, null, "workflow-1", isEnabled, nextRunAt, null, "org-1", "branch-1");

    [Fact]
    public void IsDue_NextRunInThePast_ReturnsTrue()
    {
        Assert.True(ScheduleRules.IsDue(Job(From), From.AddMinutes(1)));
    }

    [Fact]
    public void IsDue_NextRunInTheFuture_ReturnsFalse()
    {
        Assert.False(ScheduleRules.IsDue(Job(From.AddDays(1)), From));
    }

    [Fact]
    public void IsDue_DisabledJob_ReturnsFalseEvenIfOverdue()
    {
        Assert.False(ScheduleRules.IsDue(Job(From, isEnabled: false), From.AddDays(1)));
    }
}
