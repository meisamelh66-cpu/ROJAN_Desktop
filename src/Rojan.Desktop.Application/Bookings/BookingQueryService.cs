using DomainBookings = Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Application.Bookings;

/// <summary>
/// Default <see cref="IBookingQueryService"/> implementation - fetches
/// from <see cref="DomainBookings.IBookingRepository"/> (Application is
/// allowed to depend on Domain) and maps every Domain type to its
/// Application-owned equivalent via <see cref="BookingMapper"/>, so
/// nothing Domain-shaped ever crosses into Presentation.
/// </summary>
public sealed class BookingQueryService : IBookingQueryService
{
    private readonly DomainBookings.IBookingRepository _repository;

    public BookingQueryService(DomainBookings.IBookingRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<BookingDto>> GetBookingsAsync(CancellationToken cancellationToken = default)
    {
        var bookings = await _repository.GetBookingsAsync(cancellationToken).ConfigureAwait(true);
        return bookings.Select(BookingMapper.MapBooking).ToList();
    }
}
