namespace Rojan.Desktop.Domain.Bookings;

/// <summary>
/// A single booking record, as returned by <see cref="IBookingRepository"/>.
/// <see cref="CustomerId"/>, <see cref="ServiceId"/>, and
/// <see cref="SpecialistId"/> are free-form, unvalidated references - this
/// vertical slice deliberately does not depend on <c>Domain.Customers</c>,
/// <c>Domain.Services</c>, or <c>Domain.Specialists</c> (per the
/// Independence goal in docs/architecture/00-overview.md §2). Phase 15
/// populates them with real cross-slice ids/names, but that resolution and
/// coordination happens in <c>Application.BookingWorkflow</c>, not here -
/// Domain stays a dumb data shape.
/// </summary>
public sealed record Booking(
    string Id,
    string CustomerId,
    string CustomerName,
    string ServiceId,
    string ServiceName,
    string SpecialistId,
    string SpecialistName,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    string Price,
    BookingStatus Status,
    string Notes,
    string OrganizationId,
    string BranchId);
