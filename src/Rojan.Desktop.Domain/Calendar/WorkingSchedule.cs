namespace Rojan.Desktop.Domain.Calendar;

/// <summary>
/// A specialist's recurring working hours for one day of the week, as
/// returned by <see cref="ICalendarRepository"/>. <see cref="SpecialistId"/>/
/// <see cref="SpecialistName"/> are free-form, unvalidated references, same
/// reasoning as <c>Services.SpecialistService</c>: this vertical slice
/// deliberately does not depend on <c>Domain.Specialists</c> (per the
/// Independence goal in docs/architecture/00-overview.md §2) - linking to
/// a real Specialist record is a future integration point, not built here.
/// <see cref="Breaks"/> holds recurring blocked windows within these
/// working hours (e.g. lunch) - each is checked the same way as a booked
/// range, but produces <see cref="AvailabilityStatus.Unavailable"/> instead
/// of <see cref="AvailabilityStatus.Booked"/> (see
/// <c>Application.Calendar.CalendarQueryService</c>); defaults to none so
/// every pre-Sprint-2 call site keeps compiling unchanged.
/// </summary>
public sealed record WorkingSchedule(
    string Id,
    string SpecialistId,
    string SpecialistName,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    IReadOnlyList<TimeSlot>? Breaks = null)
{
    public IReadOnlyList<TimeSlot> Breaks { get; init; } = Breaks ?? [];
}
