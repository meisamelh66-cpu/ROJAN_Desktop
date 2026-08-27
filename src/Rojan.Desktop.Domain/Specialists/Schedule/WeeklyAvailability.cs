namespace Rojan.Desktop.Domain.Specialists.Schedule;

/// <summary>
/// A specialist's recurring availability for one day of the week - the
/// Official Shift definition ("specialist assigned to availability
/// window"), backed by ROJAN_Backend's <c>SpecialistScheduleController</c>
/// weekly-availability endpoints. Deliberately does not depend on
/// <see cref="Specialist"/> itself, same "free-form id reference, no hard
/// link" reasoning already used by <c>Calendar.WorkingSchedule.SpecialistId</c> -
/// this stays a thin mirror of what the backend returns, not an aggregate
/// that owns the specialist it belongs to.
/// </summary>
public sealed record WeeklyAvailability(
    string Id,
    string SpecialistId,
    DayOfWeek DayOfWeek,
    IReadOnlyList<TimeInterval> Intervals);
