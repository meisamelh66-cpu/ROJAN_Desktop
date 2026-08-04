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
        // Specialists.FakeSpecialistRepository) - "پکیج گروهی سازمانی"
        // (booking-3) predates the Service catalog (Phase 11 vs. Phase 13)
        // and isn't one of the nine seeded services, so it deliberately
        // keeps an empty ServiceId/unset Price rather than fabricating a
        // false link.
        // Phase 22A: same org-1/branch-1/branch-2, org-2/branch-3 spread as
        // Customers.FakeCustomerRepository - real mixed-tenant data for
        // Organization/Branch Scoping to filter.
        _bookings =
        [
            new Booking("booking-1", string.Empty, "لیلا احمدی", "service-2", "اصلاح رنگ ریشه", "specialist-1", "کیانا رادمنش",
                now.AddDays(2), 90, "1,200,000 تومان", BookingStatus.Confirmed, "مشتری ثابت اصلاح رنگ.", "org-1", "branch-1"),
            new Booking("booking-2", string.Empty, "سارا محمدی", "service-3", "پکیج کامل - بالیاژ و استایل", "specialist-1", "کیانا رادمنش",
                now.AddDays(5), 150, "2,200,000 تومان", BookingStatus.Pending, "مشتری ویژه - پکیج کامل ماهانه.", "org-1", "branch-1"),
            new Booking("booking-3", string.Empty, "مریم حسینی", string.Empty, "پکیج گروهی سازمانی", "specialist-2", "سارا امینی",
                now.AddDays(9), 240, "0 تومان", BookingStatus.Pending, "حساب سازمانی - تیم شش‌نفره.", "org-1", "branch-2"),
            new Booking("booking-4", string.Empty, "امیر رضایی", "service-7", "مشاوره", "specialist-2", "سارا امینی",
                now.AddDays(3), 30, "رایگان", BookingStatus.Pending, "اولین جلسه مشاوره.", "org-1", "branch-1"),
            new Booking("booking-5", string.Empty, "لیلا احمدی", "service-4", "مانیکور", "specialist-3", "مهسا کریمی",
                now.AddDays(-5), 45, "400,000 تومان", BookingStatus.Completed, string.Empty, "org-1", "branch-1"),
            new Booking("booking-6", string.Empty, "سارا محمدی", "service-5", "فیشیال ترمیمی", "specialist-3", "مهسا کریمی",
                now.AddDays(-10), 60, "850,000 تومان", BookingStatus.Completed, string.Empty, "org-1", "branch-2"),
            new Booking("booking-7", string.Empty, "علی کریمی", "service-1", "کوتاهی و استایل مو", "specialist-1", "کیانا رادمنش",
                now.AddDays(-95), 60, "650,000 تومان", BookingStatus.Completed, string.Empty, "org-1", "branch-1"),
            new Booking("booking-8", string.Empty, "رضا نوری", "service-1", "کوتاهی و استایل مو", "specialist-2", "سارا امینی",
                now.AddDays(-1), 60, "650,000 تومان", BookingStatus.Cancelled, "لغو شد - در انتظار زمان‌بندی مجدد.", "org-2", "branch-3"),
        ];
    }

    public bool SupportsInProgressAndNoShowStatuses => true;

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

    public async Task<Booking> RescheduleBookingAsync(string bookingId, DateTimeOffset newScheduledAt, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        var index = _bookings.FindIndex(existing => existing.Id == bookingId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Booking '{bookingId}' was not found.");
        }

        var updated = _bookings[index] with { ScheduledAt = newScheduledAt };
        _bookings[index] = updated;
        return updated;
    }
}
