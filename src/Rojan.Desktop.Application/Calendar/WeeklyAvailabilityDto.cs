namespace Rojan.Desktop.Application.Calendar;

/// <summary>
/// Seven consecutive <see cref="DailyAvailabilityDto"/> entries starting at
/// <see cref="WeekStart"/> for one specialist - the week-view foundation
/// query (Sprint 2 Commit 3), built from the same per-day generation/
/// conflict/break logic as <see cref="ICalendarQueryService.GetDailyAvailabilityAsync"/>,
/// not yet consumed by any Presentation view (that's Sprint 2 Commit 4).
/// </summary>
public sealed record WeeklyAvailabilityDto(
    string SpecialistId,
    string SpecialistName,
    DateOnly WeekStart,
    IReadOnlyList<DailyAvailabilityDto> Days);
