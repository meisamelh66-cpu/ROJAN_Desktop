namespace Rojan.Desktop.Application.Bookings;

/// <summary>Application-layer shape of a booking record, mapped from <see cref="Rojan.Desktop.Domain.Bookings.Booking"/> by <see cref="BookingMapper"/>.</summary>
public sealed record BookingDto(
    string Id,
    string CustomerId,
    string CustomerName,
    string ServiceName,
    string SpecialistName,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    BookingStatus Status,
    string Notes);
