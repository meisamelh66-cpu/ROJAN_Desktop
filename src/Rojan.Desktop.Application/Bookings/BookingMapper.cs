using DomainBookings = Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Application.Bookings;

/// <summary>Domain&lt;-&gt;Application mapping shared by <see cref="BookingQueryService"/> and <see cref="BookingCommandService"/> - same reasoning as <c>Customers.CustomerMapper</c>.</summary>
internal static class BookingMapper
{
    public static BookingDto MapBooking(DomainBookings.Booking booking) => new(
        booking.Id,
        booking.CustomerId,
        booking.CustomerName,
        booking.ServiceName,
        booking.SpecialistName,
        booking.ScheduledAt,
        booking.DurationMinutes,
        MapStatus(booking.Status),
        booking.Notes);

    public static BookingStatus MapStatus(DomainBookings.BookingStatus status) => status switch
    {
        DomainBookings.BookingStatus.Pending => BookingStatus.Pending,
        DomainBookings.BookingStatus.Confirmed => BookingStatus.Confirmed,
        DomainBookings.BookingStatus.Completed => BookingStatus.Completed,
        DomainBookings.BookingStatus.Cancelled => BookingStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown domain booking status."),
    };

    public static DomainBookings.BookingStatus MapStatusToDomain(BookingStatus status) => status switch
    {
        BookingStatus.Pending => DomainBookings.BookingStatus.Pending,
        BookingStatus.Confirmed => DomainBookings.BookingStatus.Confirmed,
        BookingStatus.Completed => DomainBookings.BookingStatus.Completed,
        BookingStatus.Cancelled => DomainBookings.BookingStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown application booking status."),
    };
}
