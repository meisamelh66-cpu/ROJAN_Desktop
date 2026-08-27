namespace Rojan.Desktop.Domain.Specialists.Schedule;

/// <summary>
/// A specialist's vacation/leave date range. <see cref="Reason"/> follows
/// the same backend-driven redaction rule as <see cref="ScheduleOverride.Reason"/> -
/// see that type's own doc comment.
/// </summary>
public sealed record SpecialistLeave(
    string Id,
    string SpecialistId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Reason);
