using Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Infrastructure.Bookings;

/// <summary>
/// In-memory <see cref="IBookingRepository"/> providing static sample
/// data - Phase 11 explicitly has no backend integration yet, same as
/// every other vertical slice in this app. Instance (not static) mutable
/// state, same reasoning as <c>Customers.FakeCustomerRepository</c>: this
/// fake has real create/update-status commands, so it needs to remember
/// writes for the app's lifetime - registered as a DI singleton (see
/// Infrastructure's ServiceCollectionExtensions). The small artificial
/// delays are deliberate, same reasoning as every other fake repository:
/// without them, Loading states would never actually be observable when
/// running the app.
/// </summary>
public sealed class FakeBookingRepository : IBookingRepository
{
    private readonly List<Booking> _bookings;

    public FakeBookingRepository()
    {
        var now = DateTimeOffset.Now;

        _bookings =
        [
            new Booking("booking-1", string.Empty, "Amelia Hart", "Colour Touch-Up", "Jordan Lee",
                now.AddDays(2), 90, BookingStatus.Confirmed, "Regular colour touch-up client."),
            new Booking("booking-2", string.Empty, "Sophia Reyes", "Full Package - Balayage & Style", "Jordan Lee",
                now.AddDays(5), 150, BookingStatus.Pending, "VIP tier - full package monthly."),
            new Booking("booking-3", string.Empty, "Olivia Chen", "Corporate Group Styling", "Priya Nair",
                now.AddDays(9), 240, BookingStatus.Pending, "Corporate account - team of six."),
            new Booking("booking-4", string.Empty, "Noah Bennett", "Consultation", "Priya Nair",
                now.AddDays(3), 30, BookingStatus.Pending, "First-time consultation."),
            new Booking("booking-5", string.Empty, "Amelia Hart", "Manicure", "Casey Morgan",
                now.AddDays(-5), 45, BookingStatus.Completed, string.Empty),
            new Booking("booking-6", string.Empty, "Sophia Reyes", "Facial Renewal", "Casey Morgan",
                now.AddDays(-10), 60, BookingStatus.Completed, string.Empty),
            new Booking("booking-7", string.Empty, "Liam Foster", "Haircut & Style", "Jordan Lee",
                now.AddDays(-95), 60, BookingStatus.Completed, string.Empty),
            new Booking("booking-8", string.Empty, "Ethan Brooks", "Haircut & Style", "Priya Nair",
                now.AddDays(-1), 60, BookingStatus.Cancelled, "Cancelled - rescheduling pending."),
        ];
    }

    public async Task<IReadOnlyList<Booking>> GetBookingsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(400, cancellationToken).ConfigureAwait(true);
        return _bookings.ToList();
    }

    public async Task<Booking?> GetBookingByIdAsync(string bookingId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _bookings.FirstOrDefault(booking => booking.Id == bookingId);
    }

    public async Task<Booking> CreateBookingAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _bookings.Add(booking);
        return booking;
    }

    public async Task<Booking> UpdateBookingStatusAsync(string bookingId, BookingStatus status, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        var index = _bookings.FindIndex(existing => existing.Id == bookingId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Booking '{bookingId}' was not found.");
        }

        var updated = _bookings[index] with { Status = status };
        _bookings[index] = updated;
        return updated;
    }
}
