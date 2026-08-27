namespace Rojan.Desktop.Application.Specialists.Schedule;

/// <summary>Application-layer shape of a one-off availability override, mapped from <see cref="Rojan.Desktop.Domain.Specialists.Schedule.ScheduleOverride"/>. An empty <see cref="Intervals"/> list is a real "unavailable all day" state, not an error. <see cref="Reason"/> is <see langword="null"/> whenever ROJAN_Backend itself redacted it - see the Domain type's own doc comment.</summary>
public sealed record ScheduleOverrideDto(
    string Id,
    string SpecialistId,
    DateOnly Date,
    IReadOnlyList<TimeIntervalDto> Intervals,
    string? Reason);
