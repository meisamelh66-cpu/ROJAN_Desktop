using Rojan.Desktop.Application.Bookings;

namespace Rojan.Desktop.Application.Tests.Customers;

/// <summary>Minimal <see cref="IBookingQueryService"/> test double - only <see cref="GetBookingsAsync"/> is exercised by <see cref="CustomerProfileQueryServiceTests"/>.</summary>
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

    public Task<IReadOnlyList<BookingDto>> SearchBookingsAsync(BookingSearchFilter filter, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by CustomerProfileQueryServiceTests.");
}
