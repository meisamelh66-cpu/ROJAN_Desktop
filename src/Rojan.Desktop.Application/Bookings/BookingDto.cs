namespace Rojan.Desktop.Application.Bookings;

/// <summary>Application-layer shape of a booking record, mapped from <see cref="Rojan.Desktop.Domain.Bookings.Booking"/> by <see cref="BookingMapper"/>.</summary>
public sealed record BookingDto(
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
