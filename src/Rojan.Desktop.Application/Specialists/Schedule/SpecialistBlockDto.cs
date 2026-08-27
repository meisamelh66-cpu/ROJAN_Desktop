namespace Rojan.Desktop.Application.Specialists.Schedule;

/// <summary>Application-layer shape of a specialist's ad-hoc blocked window, mapped from <see cref="Rojan.Desktop.Domain.Specialists.Schedule.SpecialistBlock"/>. <see cref="Reason"/> follows the same backend-driven redaction rule as <see cref="ScheduleOverrideDto.Reason"/>.</summary>
public sealed record SpecialistBlockDto(
    string Id,
    string SpecialistId,
    DateOnly Date,
    TimeIntervalDto Interval,
    string? Reason);
