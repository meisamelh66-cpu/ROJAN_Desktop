using Rojan.Desktop.Application.Dashboard;
using DomainDashboard = Rojan.Desktop.Domain.Dashboard;

namespace Rojan.Desktop.Application.Tests.Dashboard;

public sealed class DashboardQueryServiceTests
{
    [Fact]
    public async Task GetOverviewAsync_RepositoryReturnsData_MapsAllFieldsToDto()
    {
        var occurredAt = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
        var metric = new DomainDashboard.KpiMetric("kpi-1", "Total Bookings", "128", DomainDashboard.TrendDirection.Up, 12.5);
        var activity = new DomainDashboard.ActivityEntry("activity-1", "New booking created", occurredAt);
        var repository = new StubDashboardRepository([metric], [activity]);
        var sut = new DashboardQueryService(repository);

        var result = await sut.GetOverviewAsync();

        var metricDto = Assert.Single(result.KpiMetrics);
        Assert.Equal(metric.Id, metricDto.Id);
        Assert.Equal(metric.Label, metricDto.Label);
        Assert.Equal(metric.Value, metricDto.Value);
        Assert.Equal(TrendDirection.Up, metricDto.TrendDirection);
        Assert.Equal(metric.TrendPercentage, metricDto.TrendPercentage);

        var activityDto = Assert.Single(result.RecentActivity);
        Assert.Equal(activity.Id, activityDto.Id);
        Assert.Equal(activity.Description, activityDto.Description);
        Assert.Equal(activity.OccurredAt, activityDto.OccurredAt);
    }

    [Fact]
    public async Task GetOverviewAsync_RepositoryReturnsEmptyLists_ReturnsEmptyDto()
    {
        var repository = new StubDashboardRepository([], []);
        var sut = new DashboardQueryService(repository);

        var result = await sut.GetOverviewAsync();

        Assert.Empty(result.KpiMetrics);
        Assert.Empty(result.RecentActivity);
    }

    [Theory]
    [InlineData(DomainDashboard.TrendDirection.Up, TrendDirection.Up)]
    [InlineData(DomainDashboard.TrendDirection.Down, TrendDirection.Down)]
    [InlineData(DomainDashboard.TrendDirection.Flat, TrendDirection.Flat)]
    public async Task GetOverviewAsync_EachDomainTrendDirection_MapsToMatchingApplicationTrendDirection(
        DomainDashboard.TrendDirection domainDirection, TrendDirection expectedDirection)
    {
        var metric = new DomainDashboard.KpiMetric("kpi-1", "Metric", "1", domainDirection, 0);
        var repository = new StubDashboardRepository([metric], []);
        var sut = new DashboardQueryService(repository);

        var result = await sut.GetOverviewAsync();

        Assert.Equal(expectedDirection, Assert.Single(result.KpiMetrics).TrendDirection);
    }
}
