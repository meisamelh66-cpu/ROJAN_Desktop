namespace Rojan.Desktop.Domain.Specialists.Schedule;

/// <summary>
/// A time-of-day window (e.g. 09:00-13:00), the shared building block for
/// <see cref="WeeklyAvailability"/>/<see cref="ScheduleOverride"/>/
/// <see cref="SpecialistBlock"/> - same <see cref="TimeSpan"/>-based shape
/// already established by <c>Calendar.WorkingSchedule</c>/<c>HR.Shift</c>
/// for this exact "time of day, no date" concept, kept for consistency
/// rather than introducing <see cref="TimeOnly"/> as a second convention.
/// </summary>
public sealed record TimeInterval(TimeSpan Start, TimeSpan End);
