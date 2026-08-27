using DomainSchedule = Rojan.Desktop.Domain.Specialists.Schedule;

namespace Rojan.Desktop.Application.Specialists.Schedule;

/// <summary>Domain&lt;-&gt;Application mapping for the specialist-schedule vertical slice - same reasoning as <c>Specialists.SpecialistMapper</c>.</summary>
internal static class SpecialistScheduleMapper
{
    public static TimeIntervalDto MapInterval(DomainSchedule.TimeInterval interval) =>
        new(interval.Start, interval.End);

    public static DomainSchedule.TimeInterval MapIntervalToDomain(TimeIntervalDto interval) =>
        new(interval.Start, interval.End);

    public static WeeklyAvailabilityDto MapWeeklyAvailability(DomainSchedule.WeeklyAvailability availability) => new(
        availability.Id,
        availability.SpecialistId,
        availability.DayOfWeek,
        availability.Intervals.Select(MapInterval).ToList());

    public static ScheduleOverrideDto MapOverride(DomainSchedule.ScheduleOverride @override) => new(
        @override.Id,
        @override.SpecialistId,
        @override.Date,
        @override.Intervals.Select(MapInterval).ToList(),
        @override.Reason);

    public static SpecialistLeaveDto MapLeave(DomainSchedule.SpecialistLeave leave) => new(
        leave.Id,
        leave.SpecialistId,
        leave.StartDate,
        leave.EndDate,
        leave.Reason);

    public static SpecialistBlockDto MapBlock(DomainSchedule.SpecialistBlock block) => new(
        block.Id,
        block.SpecialistId,
        block.Date,
        MapInterval(block.Interval),
        block.Reason);
}
