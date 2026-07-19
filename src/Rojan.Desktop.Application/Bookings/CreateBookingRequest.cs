namespace Rojan.Desktop.Application.Bookings;

/// <summary>
/// Input to <see cref="IBookingCommandService.CreateBookingAsync"/> - new
/// bookings always start as <c>Pending</c>, so Status isn't a
/// caller-supplied field. The trailing four fields default to empty/zero
/// so the existing free-text quick-add form (<c>BookingPageViewModel</c>)
/// keeps compiling unchanged; <c>BookingWorkflowService</c> is the only
/// caller that populates them with real cross-slice ids and a resolved
/// price.
/// </summary>
public sealed record CreateBookingRequest(
    string CustomerName,
    string ServiceName,
    string SpecialistName,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    string Notes,
    string CustomerId = "",
    string ServiceId = "",
    string SpecialistId = "",
    string Price = "$0");
