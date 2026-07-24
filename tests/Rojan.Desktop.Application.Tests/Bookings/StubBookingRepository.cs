using Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Application.Tests.Bookings;

/// <summary>In-memory, mutable <see cref="IBookingRepository"/> test double - same reasoning as Customers.StubCustomerRepository.</summary>
internal sealed class StubBookingRepository : IBookingRepository
{
    public List<Booking> Bookings { get; } = [];

    public StubBookingRepository()
    {
    }

    public StubBookingRepository(IReadOnlyList<Booking> bookings)
    {
        Bookings.AddRange(bookings);
    }

    public Task<IReadOnlyList<Booking>> GetBookingsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Booking>>(Bookings.ToList());

    public Task<Booking?> GetBookingByIdAsync(string bookingId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Bookings.FirstOrDefault(booking => booking.Id == bookingId));

    public Task<Booking> CreateBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        Bookings.Add(booking);
        return Task.FromResult(booking);
    }

    public Task<Booking> UpdateBookingStatusAsync(string bookingId, BookingStatus status, CancellationToken cancellationToken = default)
    {
        var index = Bookings.FindIndex(booking => booking.Id == bookingId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Booking '{bookingId}' was not found.");
        }

        var updated = Bookings[index] with { Status = status };
        Bookings[index] = updated;
        return Task.FromResult(updated);
    }

    public Task<Booking> RescheduleBookingAsync(string bookingId, DateTimeOffset newScheduledAt, CancellationToken cancellationToken = default)
    {
        var index = Bookings.FindIndex(booking => booking.Id == bookingId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Booking '{bookingId}' was not found.");
        }

        var updated = Bookings[index] with { ScheduledAt = newScheduledAt };
        Bookings[index] = updated;
        return Task.FromResult(updated);
    }
}
