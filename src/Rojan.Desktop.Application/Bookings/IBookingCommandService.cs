namespace Rojan.Desktop.Application.Bookings;

/// <summary>Write use cases for Bookings - same command-side pattern <c>Customers.ICustomerCommandService</c> established in Phase 10.</summary>
public interface IBookingCommandService
{
    public Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken = default);

    public Task<BookingDto> UpdateBookingStatusAsync(string bookingId, BookingStatus status, CancellationToken cancellationToken = default);

    /// <summary>Moves an existing active booking to <paramref name="newScheduledAt"/> - rejects a terminal-status booking and rejects a specialist/time conflict with another active booking, the same double-booking guard <see cref="CreateBookingAsync"/> already enforces (Sprint 3 Commit 6). Calendar slot release/reservation is <c>BookingWorkflow.IBookingWorkflowService.RescheduleBookingAsync</c>'s job, not this method's - this only moves the booking record itself.</summary>
    public Task<BookingDto> RescheduleBookingAsync(string bookingId, DateTimeOffset newScheduledAt, CancellationToken cancellationToken = default);
}
