namespace Rojan.Desktop.Application.Specialists.Schedule;

/// <summary>Application-layer shape of a specialist's recurring weekly availability for one day, mapped from <see cref="Rojan.Desktop.Domain.Specialists.Schedule.WeeklyAvailability"/> by <see cref="SpecialistScheduleMapper"/>.</summary>
public sealed record WeeklyAvailabilityDto(
    string Id,
    string SpecialistId,
    DayOfWeek DayOfWeek,
    IReadOnlyList<TimeIntervalDto> Intervals);
