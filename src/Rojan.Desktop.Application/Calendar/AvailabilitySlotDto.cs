namespace Rojan.Desktop.Application.Calendar;

/// <summary>Application-layer shape of a generated availability slot, mapped from <see cref="Rojan.Desktop.Domain.Calendar.AvailabilitySlot"/> by <see cref="CalendarMapper"/>.</summary>
public sealed record AvailabilitySlotDto(
    string SpecialistId,
    string SpecialistName,
    DateTimeOffset Start,
    DateTimeOffset End,
    AvailabilityStatus Status);
