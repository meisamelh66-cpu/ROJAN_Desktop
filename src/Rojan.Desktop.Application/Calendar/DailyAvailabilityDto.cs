namespace Rojan.Desktop.Application.Calendar;

/// <summary>
/// Everything the daily availability screen needs for one specialist on
/// one day - the working-hours window (null when the specialist doesn't
/// work that day, in which case <see cref="Slots"/> is empty) plus every
/// generated slot, each already marked Available/Booked. The aggregate
/// fetched together as a single unit of work, same reasoning as
/// Customers.CustomerProfileDto.
/// </summary>
public sealed record DailyAvailabilityDto(
    string SpecialistId,
    string SpecialistName,
    DateOnly Date,
    TimeSpan? WorkingStart,
    TimeSpan? WorkingEnd,
    IReadOnlyList<AvailabilitySlotDto> Slots);
