namespace Rojan.Desktop.Domain.Bookings;

/// <summary>
/// Repository abstraction for booking data. Domain defines the contract;
/// Infrastructure provides the concrete implementation (a fake/in-memory
/// one for now - Phase 11 explicitly has no backend integration yet, same
/// as every other vertical slice in this app).
/// </summary>
public interface IBookingRepository
{
    /// <summary>
    /// Owner App Booking Integration: <see langword="true"/> for a
    /// repository that can actually persist <see cref="BookingStatus.InProgress"/>/
    /// <see cref="BookingStatus.NoShow"/> (every local implementation);
    /// <see langword="false"/> for a backend-connected one, since
    /// ROJAN_Backend's own <c>BookingStatus</c> has only 4 states
    /// (<c>PENDING/CONFIRMED/CANCELLED/COMPLETED</c>) with no equivalent
    /// for either. <c>BookingPageViewModel</c> consults this (via
    /// <c>IBookingCommandService</c>) to disable the Start/No-Show actions
    /// rather than let them fail at the repository call.
    /// </summary>
    public bool SupportsInProgressAndNoShowStatuses { get; }

    public Task<IReadOnlyList<Booking>> GetBookingsAsync(CancellationToken cancellationToken = default);

    public Task<Booking?> GetBookingByIdAsync(string bookingId, CancellationToken cancellationToken = default);

    public Task<Booking> CreateBookingAsync(Booking booking, CancellationToken cancellationToken = default);

    public Task<Booking> UpdateBookingStatusAsync(string bookingId, BookingStatus status, CancellationToken cancellationToken = default);

    /// <summary>Moves an existing booking's <see cref="Booking.ScheduledAt"/> to <paramref name="newScheduledAt"/> - Sprint 3 Commit 6. Eligibility (only active bookings) and scheduling-conflict validation happen in Application, not here - same "return/mutate the raw record, validate in Application" convention <see cref="UpdateBookingStatusAsync"/> already follows.</summary>
    public Task<Booking> RescheduleBookingAsync(string bookingId, DateTimeOffset newScheduledAt, CancellationToken cancellationToken = default);
}
