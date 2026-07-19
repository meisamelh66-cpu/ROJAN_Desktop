namespace Rojan.Desktop.Domain.Bookings;

/// <summary>
/// Repository abstraction for booking data. Domain defines the contract;
/// Infrastructure provides the concrete implementation (a fake/in-memory
/// one for now - Phase 11 explicitly has no backend integration yet, same
/// as every other vertical slice in this app).
/// </summary>
public interface IBookingRepository
{
    public Task<IReadOnlyList<Booking>> GetBookingsAsync(CancellationToken cancellationToken = default);

    public Task<Booking?> GetBookingByIdAsync(string bookingId, CancellationToken cancellationToken = default);

    public Task<Booking> CreateBookingAsync(Booking booking, CancellationToken cancellationToken = default);

    public Task<Booking> UpdateBookingStatusAsync(string bookingId, BookingStatus status, CancellationToken cancellationToken = default);
}
