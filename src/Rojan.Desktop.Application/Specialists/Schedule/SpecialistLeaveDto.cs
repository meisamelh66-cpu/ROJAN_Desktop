namespace Rojan.Desktop.Application.Specialists.Schedule;

/// <summary>Application-layer shape of a specialist's leave record, mapped from <see cref="Rojan.Desktop.Domain.Specialists.Schedule.SpecialistLeave"/>. <see cref="Reason"/> follows the same backend-driven redaction rule as <see cref="ScheduleOverrideDto.Reason"/>.</summary>
public sealed record SpecialistLeaveDto(
    string Id,
    string SpecialistId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Reason);
