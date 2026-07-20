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

        // ServiceId/SpecialistId are populated only where the booking's
        // free-text ServiceName/SpecialistName cleanly matches a real
        // catalog entry (Services.FakeServiceRepository /
        // Specialists.FakeSpecialistRepository) - "Corporate Group
        // Styling" (booking-3) predates the Service catalog (Phase 11
        // vs. Phase 13) and isn't one of the nine seeded services, so it
        // deliberately keeps an empty ServiceId/"$0" Price rather than
        // fabricating a false link.
        // Phase 22A: same org-1/branch-1/branch-2, org-2/branch-3 spread as
        // Customers.FakeCustomerRepository - real mixed-tenant data for
        // Organization/Branch Scoping to filter.
        _bookings =
        [
            new Booking("booking-1", string.Empty, "Amelia Hart", "service-2", "Colour Touch-Up", "specialist-1", "Jordan Lee",
                now.AddDays(2), 90, "$120", BookingStatus.Confirmed, "Regular colour touch-up client.", "org-1", "branch-1"),
            new Booking("booking-2", string.Empty, "Sophia Reyes", "service-3", "Full Package - Balayage & Style", "specialist-1", "Jordan Lee",
                now.AddDays(5), 150, "$220", BookingStatus.Pending, "VIP tier - full package monthly.", "org-1", "branch-1"),
            new Booking("booking-3", string.Empty, "Olivia Chen", string.Empty, "Corporate Group Styling", "specialist-2", "Priya Nair",
                now.AddDays(9), 240, "$0", BookingStatus.Pending, "Corporate account - team of six.", "org-1", "branch-2"),
            new Booking("booking-4", string.Empty, "Noah Bennett", "service-7", "Consultation", "specialist-2", "Priya Nair",
                now.AddDays(3), 30, "$0", BookingStatus.Pending, "First-time consultation.", "org-1", "branch-1"),
            new Booking("booking-5", string.Empty, "Amelia Hart", "service-4", "Manicure", "specialist-3", "Casey Morgan",
                now.AddDays(-5), 45, "$40", BookingStatus.Completed, string.Empty, "org-1", "branch-1"),
            new Booking("booking-6", string.Empty, "Sophia Reyes", "service-5", "Facial Renewal", "specialist-3", "Casey Morgan",
                now.AddDays(-10), 60, "$85", BookingStatus.Completed, string.Empty, "org-1", "branch-2"),
            new Booking("booking-7", string.Empty, "Liam Foster", "service-1", "Haircut & Style", "specialist-1", "Jordan Lee",
                now.AddDays(-95), 60, "$65", BookingStatus.Completed, string.Empty, "org-1", "branch-1"),
            new Booking("booking-8", string.Empty, "Ethan Brooks", "service-1", "Haircut & Style", "specialist-2", "Priya Nair",
                now.AddDays(-1), 60, "$65", BookingStatus.Cancelled, "Cancelled - rescheduling pending.", "org-2", "branch-3"),
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
