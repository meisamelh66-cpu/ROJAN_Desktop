namespace Rojan.Desktop.Domain.Calendar;

/// <summary>A concrete time range on a specific day - no identity of its own, a pure value used both for existing booked ranges (<see cref="ICalendarRepository.GetBookedSlotsAsync"/>) and generated availability.</summary>
public sealed record TimeSlot(DateTimeOffset Start, DateTimeOffset End);
