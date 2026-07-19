using DomainBookings = Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Application.Bookings;

/// <summary>
/// Default <see cref="IBookingCommandService"/> implementation. Enforces
/// <see cref="DomainBookings.BookingRules"/> on every write - an invalid
/// duration or an illegal status transition throws rather than silently
/// writing bad data, now that a real cross-slice workflow
/// (<c>BookingWorkflowService</c>) depends on this service's guarantees.
/// </summary>
public sealed class BookingCommandService : IBookingCommandService
{
    private readonly DomainBookings.IBookingRepository _repository;

    public BookingCommandService(DomainBookings.IBookingRepository repository)
    {
        _repository = repository;
    }

    public async Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        if (!DomainBookings.BookingRules.IsValidDuration(request.DurationMinutes))
        {
            throw new ArgumentException($"Duration {request.DurationMinutes} minutes is not valid.", nameof(request));
        }

        var booking = new DomainBookings.Booking(
            Guid.NewGuid().ToString(),
            request.CustomerId,
            request.CustomerName,
            request.ServiceId,
            request.ServiceName,
            request.SpecialistId,
            request.SpecialistName,
            request.ScheduledAt,
            request.DurationMinutes,
            request.Price,
            DomainBookings.BookingStatus.Pending,
            request.Notes);

        var created = await _repository.CreateBookingAsync(booking, cancellationToken).ConfigureAwait(true);
        return BookingMapper.MapBooking(created);
    }

    public async Task<BookingDto> UpdateBookingStatusAsync(string bookingId, BookingStatus status, CancellationToken cancellationToken = default)
    {
        var current = await _repository.GetBookingByIdAsync(bookingId, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Booking '{bookingId}' was not found.");

        var domainStatus = BookingMapper.MapStatusToDomain(status);
        if (!DomainBookings.BookingRules.IsValidTransition(current.Status, domainStatus))
        {
            throw new InvalidOperationException($"Cannot transition booking from {current.Status} to {domainStatus}.");
        }

        var updated = await _repository
            .UpdateBookingStatusAsync(bookingId, domainStatus, cancellationToken)
            .ConfigureAwait(true);
        return BookingMapper.MapBooking(updated);
    }
}
