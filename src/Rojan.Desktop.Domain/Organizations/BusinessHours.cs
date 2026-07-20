namespace Rojan.Desktop.Domain.Organizations;

/// <summary>A branch's daily open/close time - one value shared across every <see cref="BranchSettings.WorkingDays"/> entry (this phase does not model per-day-of-week hours).</summary>
public sealed record BusinessHours(TimeOnly OpenTime, TimeOnly CloseTime);
