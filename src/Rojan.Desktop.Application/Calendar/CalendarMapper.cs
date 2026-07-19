using DomainCalendar = Rojan.Desktop.Domain.Calendar;

namespace Rojan.Desktop.Application.Calendar;

/// <summary>Domain&lt;-&gt;Application mapping shared by every Calendar use case - same reasoning as <c>Customers.CustomerMapper</c>.</summary>
internal static class CalendarMapper
{
    public static AvailabilitySlotDto MapSlot(DomainCalendar.AvailabilitySlot slot) => new(
        slot.SpecialistId,
        slot.SpecialistName,
        slot.Start,
        slot.End,
        MapStatus(slot.Status));

    public static AvailabilityStatus MapStatus(DomainCalendar.AvailabilityStatus status) => status switch
    {
        DomainCalendar.AvailabilityStatus.Available => AvailabilityStatus.Available,
        DomainCalendar.AvailabilityStatus.Booked => AvailabilityStatus.Booked,
        DomainCalendar.AvailabilityStatus.Unavailable => AvailabilityStatus.Unavailable,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown domain availability status."),
    };
}
