using Rojan.Desktop.Application.Bookings;

namespace Rojan.Desktop.Presentation.Tests.Bookings;

/// <summary>Configurable <see cref="IBookingQueryService"/> test double - same reasoning as Customers.StubCustomerQueryService.</summary>
internal sealed class StubBookingQueryService : IBookingQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<BookingDto>>> _getBookings;

    public StubBookingQueryService(Func<CancellationToken, Task<IReadOnlyList<BookingDto>>> getBookings)
    {
        _getBookings = getBookings;
    }

    public Task<IReadOnlyList<BookingDto>> GetBookingsAsync(CancellationToken cancellationToken = default) =>
        _getBookings(cancellationToken);

    public async Task<BookingDto?> GetBookingByIdAsync(string bookingId, CancellationToken cancellationToken = default)
    {
        var bookings = await _getBookings(cancellationToken).ConfigureAwait(true);
        return bookings.FirstOrDefault(booking => booking.Id == bookingId);
    }
}
