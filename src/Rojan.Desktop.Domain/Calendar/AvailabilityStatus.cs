namespace Rojan.Desktop.Domain.Calendar;

/// <summary>
/// State of a generated time slot, as returned by <see cref="ICalendarRepository"/>-backed
/// generation logic. <c>Unavailable</c> is part of the state space this
/// phase's slot generator does not currently emit (every generated slot
/// falls within working hours, so it is only ever Available or Booked) -
/// reserved for a future refinement (e.g. breaks/blocked time) rather than
/// removed, since the enum should model the full domain concept.
/// </summary>
public enum AvailabilityStatus
{
    Available,
    Booked,
    Unavailable,
}
