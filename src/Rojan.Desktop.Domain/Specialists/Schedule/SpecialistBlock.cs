namespace Rojan.Desktop.Domain.Specialists.Schedule;

/// <summary>
/// An ad-hoc blocked time window for a specialist on a specific date (e.g.
/// a one-off appointment elsewhere) - narrower than <see cref="ScheduleOverride"/>,
/// which replaces the whole day's availability; a block instead removes one
/// window from it. <see cref="Reason"/> follows the same backend-driven
/// redaction rule as <see cref="ScheduleOverride.Reason"/> - see that
/// type's own doc comment.
/// </summary>
public sealed record SpecialistBlock(
    string Id,
    string SpecialistId,
    DateOnly Date,
    TimeInterval Interval,
    string? Reason);
