namespace Rojan.Desktop.Application.Bookings;

/// <summary>Input to <see cref="IBookingCommandService.CreateBookingAsync"/> - new bookings always start as <c>Pending</c>, so Status isn't a caller-supplied field.</summary>
public sealed record CreateBookingRequest(
    string CustomerName,
    string ServiceName,
    string SpecialistName,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    string Notes);
