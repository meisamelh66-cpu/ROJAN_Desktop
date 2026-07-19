namespace Rojan.Desktop.Domain.Bookings;

/// <summary>Lifecycle stage of a booking, as returned by <see cref="IBookingRepository"/>. Legal transitions between these are defined by <see cref="BookingRules.IsValidTransition"/>, not encoded here.</summary>
public enum BookingStatus
{
    Pending,
    Confirmed,
    InProgress,
    Completed,
    Cancelled,
    NoShow,
}
