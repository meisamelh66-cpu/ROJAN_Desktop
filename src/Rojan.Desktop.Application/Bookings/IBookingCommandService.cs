namespace Rojan.Desktop.Application.Bookings;

/// <summary>Write use cases for Bookings - same command-side pattern <c>Customers.ICustomerCommandService</c> established in Phase 10.</summary>
public interface IBookingCommandService
{
    public Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken = default);

    public Task<BookingDto> UpdateBookingStatusAsync(string bookingId, BookingStatus status, CancellationToken cancellationToken = default);
}
