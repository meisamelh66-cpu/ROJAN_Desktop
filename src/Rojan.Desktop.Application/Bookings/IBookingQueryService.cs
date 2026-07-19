namespace Rojan.Desktop.Application.Bookings;

/// <summary>Read-only use case Presentation depends on to load Bookings - the only way Presentation ever reaches booking data, never through Domain/Infrastructure directly.</summary>
public interface IBookingQueryService
{
    public Task<IReadOnlyList<BookingDto>> GetBookingsAsync(CancellationToken cancellationToken = default);
}
