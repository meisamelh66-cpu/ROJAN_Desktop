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

    /// <summary>Moves an existing booking's <see cref="Booking.ScheduledAt"/> to <paramref name="newScheduledAt"/> - Sprint 3 Commit 6. Eligibility (only active bookings) and scheduling-conflict validation happen in Application, not here - same "return/mutate the raw record, validate in Application" convention <see cref="UpdateBookingStatusAsync"/> already follows.</summary>
    public Task<Booking> RescheduleBookingAsync(string bookingId, DateTimeOffset newScheduledAt, CancellationToken cancellationToken = default);
}
