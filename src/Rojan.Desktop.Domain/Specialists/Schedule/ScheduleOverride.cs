namespace Rojan.Desktop.Domain.Specialists.Schedule;

/// <summary>
/// A one-off exception to a specialist's <see cref="WeeklyAvailability"/>
/// for a specific date - an empty <see cref="Intervals"/> list is a real,
/// meaningful state ("unavailable all day on this date"), not an empty/
/// error result. <see cref="Reason"/> is <see langword="null"/> whenever
/// ROJAN_Backend itself redacted it (OWASP API3 mitigation, enforced
/// server-side for any viewer who isn't the owner/manager or the
/// specialist themself) - this type must never attempt to backfill or
/// guess a reason the backend chose not to disclose.
/// </summary>
public sealed record ScheduleOverride(
    string Id,
    string SpecialistId,
    DateOnly Date,
    IReadOnlyList<TimeInterval> Intervals,
    string? Reason);
