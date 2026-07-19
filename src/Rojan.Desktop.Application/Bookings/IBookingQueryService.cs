namespace Rojan.Desktop.Application.Bookings;

/// <summary>Read-only use case Presentation depends on to load Bookings - the only way Presentation ever reaches booking data, never through Domain/Infrastructure directly.</summary>
public interface IBookingQueryService
{
    public Task<IReadOnlyList<BookingDto>> GetBookingsAsync(CancellationToken cancellationToken = default);

    /// <summary>Single-booking lookup - backs <c>BookingWorkflowService.CancelBookingAsync</c>, which needs a booking's specialist/schedule to release the matching calendar slot. Returns null if no booking with that id exists.</summary>
    public Task<BookingDto?> GetBookingByIdAsync(string bookingId, CancellationToken cancellationToken = default);
}
