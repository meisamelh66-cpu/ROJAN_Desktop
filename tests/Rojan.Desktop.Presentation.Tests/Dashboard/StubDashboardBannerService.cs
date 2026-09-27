using Rojan.Desktop.Application.Banners;

namespace Rojan.Desktop.Presentation.Tests.Dashboard;

internal sealed class StubDashboardBannerService(Func<CancellationToken, Task<DashboardBannerDto?>> getBanner) : IDashboardBannerService
{
    public Task<DashboardBannerDto?> GetDashboardBannerAsync(CancellationToken cancellationToken = default) => getBanner(cancellationToken);
}
