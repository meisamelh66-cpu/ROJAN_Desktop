using Rojan.Desktop.Application.Bookings;

namespace Rojan.Desktop.Application.Tests.HR;

/// <summary>Minimal <see cref="IBookingQueryService"/> test double - only <see cref="GetBookingByIdAsync"/> is exercised by <see cref="CommissionCommandServiceTests"/>.</summary>
internal sealed class StubBookingQueryService : IBookingQueryService
{
    private readonly IReadOnlyList<BookingDto> _bookings;

    public StubBookingQueryService(IReadOnlyList<BookingDto> bookings)
    {
        _bookings = bookings;
    }

    public Task<IReadOnlyList<BookingDto>> GetBookingsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_bookings);

    public Task<BookingDto?> GetBookingByIdAsync(string bookingId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_bookings.FirstOrDefault(booking => booking.Id == bookingId));
}
