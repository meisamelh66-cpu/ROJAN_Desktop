using AppBookings = Rojan.Desktop.Application.Bookings;

namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Customer 360's booking-history summary - reuses <see cref="AppBookings.BookingDto"/>
/// directly rather than duplicating any <c>Domain.Bookings</c> mapping/logic
/// here; <see cref="CustomerProfileQueryService"/> builds this by composing
/// over the existing <see cref="AppBookings.IBookingQueryService.GetBookingsAsync"/>
/// and filtering by <c>CustomerId</c>, the same "Application composes over a
/// sibling module's query service" shape <c>Reporting.KpiEngineQueryService</c>
/// already establishes. <see cref="UpcomingAppointments"/>/<see cref="PastAppointments"/>
/// are split by <see cref="AppBookings.BookingDto.ScheduledAt"/> relative to
/// "now" - upcoming ordered soonest-first, past ordered most-recent-first. A
/// customer with no bookings gets <see cref="Empty"/>: two empty lists and
/// zeroed counters, never a failure.
/// </summary>
public sealed record CustomerBookingSummaryDto(
    IReadOnlyList<AppBookings.BookingDto> UpcomingAppointments,
    IReadOnlyList<AppBookings.BookingDto> PastAppointments,
    int TotalBookingCount,
    int CompletedBookingCount,
    DateTimeOffset? LastAppointmentDate)
{
    public static CustomerBookingSummaryDto Empty { get; } = new([], [], 0, 0, null);
}
