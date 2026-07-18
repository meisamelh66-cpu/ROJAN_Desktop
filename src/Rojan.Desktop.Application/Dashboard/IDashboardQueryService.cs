namespace Rojan.Desktop.Application.Dashboard;

/// <summary>Read-only use case Presentation depends on to load the Dashboard - the only way Presentation ever reaches dashboard data, never through Domain/Infrastructure directly.</summary>
public interface IDashboardQueryService
{
    public Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
}
