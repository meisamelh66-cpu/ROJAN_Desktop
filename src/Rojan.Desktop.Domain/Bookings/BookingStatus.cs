namespace Rojan.Desktop.Domain.Bookings;

/// <summary>Lifecycle stage of a booking, as returned by <see cref="IBookingRepository"/>.</summary>
public enum BookingStatus
{
    Pending,
    Confirmed,
    Completed,
    Cancelled,
}
