using DomainBookings = Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Application.Bookings;

/// <summary>Default <see cref="IBookingCommandService"/> implementation.</summary>
public sealed class BookingCommandService : IBookingCommandService
{
    private readonly DomainBookings.IBookingRepository _repository;

    public BookingCommandService(DomainBookings.IBookingRepository repository)
    {
        _repository = repository;
    }

    public async Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        var booking = new DomainBookings.Booking(
            Guid.NewGuid().ToString(),
            string.Empty,
            request.CustomerName,
            request.ServiceName,
            request.SpecialistName,
            request.ScheduledAt,
            request.DurationMinutes,
            DomainBookings.BookingStatus.Pending,
            request.Notes);

        var created = await _repository.CreateBookingAsync(booking, cancellationToken).ConfigureAwait(true);
        return BookingMapper.MapBooking(created);
    }

    public async Task<BookingDto> UpdateBookingStatusAsync(string bookingId, BookingStatus status, CancellationToken cancellationToken = default)
    {
        var updated = await _repository
            .UpdateBookingStatusAsync(bookingId, BookingMapper.MapStatusToDomain(status), cancellationToken)
            .ConfigureAwait(true);
        return BookingMapper.MapBooking(updated);
    }
}
