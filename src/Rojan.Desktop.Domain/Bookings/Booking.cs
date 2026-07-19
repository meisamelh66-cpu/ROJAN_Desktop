namespace Rojan.Desktop.Domain.Bookings;

/// <summary>
/// A single booking record, as returned by <see cref="IBookingRepository"/>.
/// <see cref="CustomerId"/> is a free-form, unvalidated reference - this
/// vertical slice deliberately does not depend on <c>Domain.Customers</c>
/// (per the Independence goal in docs/architecture/00-overview.md §2);
/// linking a booking to a real customer record is a future integration
/// point, not built here.
/// </summary>
public sealed record Booking(
    string Id,
    string CustomerId,
    string CustomerName,
    string ServiceName,
    string SpecialistName,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    BookingStatus Status,
    string Notes);
