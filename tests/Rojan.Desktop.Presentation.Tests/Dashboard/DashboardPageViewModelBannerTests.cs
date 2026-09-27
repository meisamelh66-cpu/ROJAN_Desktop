using Rojan.Desktop.Application.Banners;
using Rojan.Desktop.Application.Dashboard;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Presentation.Organizations;
using Rojan.Desktop.Presentation.Tests.Automation;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Dashboard;

/// <summary>
/// The admin-managed Dashboard hero banner: shown only when a banner is published, loaded
/// independently of the KPIs, and never able to break the rest of the Dashboard.
/// </summary>
public sealed class DashboardPageViewModelBannerTests
{
    private static readonly DashboardOverviewDto Overview = new(
        [new KpiMetricDto("kpi-1", "Metric 1", "1", TrendDirection.Flat, 0)],
        [new ActivityEntryDto("activity-1", "Event 1", DateTimeOffset.UnixEpoch)]);

    private static DashboardPageViewModel CreateSut(Func<CancellationToken, Task<DashboardBannerDto?>> getBanner, Func<CancellationToken, Task<DashboardOverviewDto>>? getOverview = null) =>
        new(
            new StubDashboardQueryService(getOverview ?? (_ => Task.FromResult(Overview))),
            new PermissionEngine(),
            new FakeCurrentSessionService(),
            new StubDashboardBannerService(getBanner));

    [Fact]
    public void PublishedBanner_IsVisibleWithItsContent()
    {
        var image = new byte[] { 1, 2, 3 };
        var sut = CreateSut(_ => Task.FromResult<DashboardBannerDto?>(new("banner-1", "Title", "Subtitle", "https://rojan.example/offer", image)));

        Assert.True(sut.IsBannerVisible);
        Assert.Equal("Title", sut.BannerTitle);
        Assert.Equal("Subtitle", sut.BannerSubtitle);
        Assert.Same(image, sut.BannerImageBytes);
        Assert.True(sut.HasBannerImage);
        Assert.Equal(new Uri("https://rojan.example/offer"), sut.BannerCtaUri);
        Assert.True(sut.HasBannerCta);
    }

    [Fact]
    public void NoPublishedBanner_BannerIsHidden()
    {
        var sut = CreateSut(_ => Task.FromResult<DashboardBannerDto?>(null));

        Assert.False(sut.IsBannerVisible);
        Assert.False(sut.HasBannerCta);
        Assert.Null(sut.BannerImageBytes);
    }

    [Fact]
    public void BannerLoadFails_BannerIsHidden_AndTheKpisStillLoad()
    {
        var sut = CreateSut(_ => Task.FromException<DashboardBannerDto?>(new InvalidOperationException("banner endpoint down")));

        Assert.False(sut.IsBannerVisible);
        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Null(sut.ErrorMessage);
    }

    [Fact]
    public void BannerStillLoading_DoesNotBlockTheKpis()
    {
        var pending = new TaskCompletionSource<DashboardBannerDto?>();
        var sut = CreateSut(_ => pending.Task);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.False(sut.IsBannerVisible);

        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);
        pending.SetResult(new DashboardBannerDto("banner-1", "Title", null, null, null));

        Assert.True(sut.IsBannerVisible);
        Assert.Contains(nameof(DashboardPageViewModel.IsBannerVisible), raised);
    }

    [Fact]
    public void KpisFail_BannerStillShows()
    {
        var sut = CreateSut(
            _ => Task.FromResult<DashboardBannerDto?>(new("banner-1", "Title", null, null, null)),
            _ => Task.FromException<DashboardOverviewDto>(new InvalidOperationException("boom")));

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.True(sut.IsBannerVisible);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/bookings")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    [InlineData("ftp://rojan.example/file")]
    public void CtaTargetNotAnAbsoluteHttpUrl_CtaIsHidden(string? href)
    {
        var sut = CreateSut(_ => Task.FromResult<DashboardBannerDto?>(new("banner-1", "Title", null, href, null)));

        Assert.True(sut.IsBannerVisible);
        Assert.Null(sut.BannerCtaUri);
        Assert.False(sut.HasBannerCta);
    }

    [Fact]
    public void BannerWithoutImage_ReportsNoImage_SoTheViewShowsItsFallbackSurface()
    {
        var sut = CreateSut(_ => Task.FromResult<DashboardBannerDto?>(new("banner-1", "Title", null, null, [])));

        Assert.True(sut.IsBannerVisible);
        Assert.False(sut.HasBannerImage);
    }
}
