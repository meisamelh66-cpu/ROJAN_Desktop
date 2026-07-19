using Rojan.Desktop.Application.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Dashboard;

/// <summary>
/// Configurable <see cref="IDashboardQueryService"/> test double. Each test
/// supplies exactly the completion behavior it needs - see
/// DashboardPageViewModelTests for the Loading/Loaded/Empty/Error cases.
/// </summary>
internal sealed class StubDashboardQueryService : IDashboardQueryService
{
    private readonly Func<CancellationToken, Task<DashboardOverviewDto>> _getOverview;

    public StubDashboardQueryService(Func<CancellationToken, Task<DashboardOverviewDto>> getOverview)
    {
        _getOverview = getOverview;
    }

    public Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default) =>
        _getOverview(cancellationToken);
}
