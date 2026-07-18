namespace Rojan.Desktop.Application.Dashboard;

/// <summary>Application-layer shape of a recent-activity record, mapped from <see cref="Rojan.Desktop.Domain.Dashboard.ActivityEntry"/> by <see cref="DashboardQueryService"/>.</summary>
public sealed record ActivityEntryDto(string Id, string Description, DateTimeOffset OccurredAt);
