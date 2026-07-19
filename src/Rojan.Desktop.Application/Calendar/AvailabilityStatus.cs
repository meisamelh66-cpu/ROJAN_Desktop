namespace Rojan.Desktop.Application.Calendar;

/// <summary>
/// Application's own copy of <see cref="Rojan.Desktop.Domain.Calendar.AvailabilityStatus"/> -
/// distinct from Domain, same reasoning as <c>Customers.CustomerStatus</c>:
/// Presentation never binds to a Domain-shaped type, so anything it needs
/// gets an Application-owned equivalent, mapped explicitly by
/// <see cref="CalendarMapper"/>.
/// </summary>
public enum AvailabilityStatus
{
    Available,
    Booked,
    Unavailable,
}
