namespace Rojan.Desktop.Domain.Dashboard;

/// <summary>A single recent-activity record, as returned by <see cref="IDashboardRepository"/>.</summary>
public sealed record ActivityEntry(string Id, string Description, DateTimeOffset OccurredAt);
