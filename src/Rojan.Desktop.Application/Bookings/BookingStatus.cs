namespace Rojan.Desktop.Application.Bookings;

/// <summary>
/// Application's own copy of <see cref="Rojan.Desktop.Domain.Bookings.BookingStatus"/> -
/// distinct from Domain, same reasoning as <c>Customers.CustomerStatus</c>:
/// Presentation never binds to a Domain-shaped type, so anything it needs
/// gets an Application-owned equivalent, mapped explicitly by
/// <see cref="BookingMapper"/>.
/// </summary>
public enum BookingStatus
{
    Pending,
    Confirmed,
    Completed,
    Cancelled,
}
