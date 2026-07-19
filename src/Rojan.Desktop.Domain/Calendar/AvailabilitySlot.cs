namespace Rojan.Desktop.Domain.Calendar;

/// <summary>A single generated availability slot for one specialist - a <see cref="TimeSlot"/> plus who it belongs to and whether it is open or already taken.</summary>
public sealed record AvailabilitySlot(
    string SpecialistId,
    string SpecialistName,
    DateTimeOffset Start,
    DateTimeOffset End,
    AvailabilityStatus Status);
