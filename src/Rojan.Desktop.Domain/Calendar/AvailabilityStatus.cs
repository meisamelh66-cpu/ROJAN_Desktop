namespace Rojan.Desktop.Domain.Calendar;

/// <summary>
/// State of a generated time slot, as returned by <see cref="ICalendarRepository"/>-backed
/// generation logic. <c>Unavailable</c> is produced when a generated slot
/// overlaps one of the specialist's <see cref="WorkingSchedule.Breaks"/> for
/// that day (see <c>Application.Calendar.CalendarQueryService</c>) - a
/// break takes precedence over an overlapping booked range, since it
/// represents blocked-by-policy time, not a real reservation to release.
/// </summary>
public enum AvailabilityStatus
{
    Available,
    Booked,
    Unavailable,
}
