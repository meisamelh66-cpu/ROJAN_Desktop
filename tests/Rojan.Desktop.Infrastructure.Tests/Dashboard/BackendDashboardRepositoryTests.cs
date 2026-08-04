using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Domain.Dashboard;
using Rojan.Desktop.Infrastructure.Dashboard;

namespace Rojan.Desktop.Infrastructure.Tests.Dashboard;

/// <summary>Exercises <see cref="BackendDashboardRepository"/>'s mapping from the real backend response onto <see cref="KpiMetric"/>/<see cref="ActivityEntry"/> - see that class's own doc comment for the two deliberate honesty notes this suite verifies (no fabricated bookings/customers trend, recommendations surfaced as activity).</summary>
public sealed class BackendDashboardRepositoryTests
{
    private static readonly DashboardInsightsResponse SampleInsights = new(
        Revenue: new DashboardRevenueResponse(Today: 150.00m, Month: 3200.00m, GrowthRate: 18.50m),
        Bookings: new DashboardBookingCountsResponse(Total: 42, Completed: 35, Cancelled: 4),
        Customers: new DashboardCustomerCountsResponse(NewCustomers: 6, ReturningCustomers: 12),
        Services: [new DashboardServiceInsightResponse("Haircut", 20, 2000.00m)],
        Recommendations:
        [
            new DashboardRecommendationResponse("REVENUE_GROWTH", "MEDIUM", "درآمد شما نسبت به دوره قبل رشد داشته است."),
            new DashboardRecommendationResponse("SERVICE_PERFORMANCE", "LOW", "پرمخاطب‌ترین خدمت این ماه: Haircut"),
        ]);

    [Fact]
    public async Task GetKpiMetricsAsync_MapsRevenueBookingsAndCustomersFromTheRealResponse()
    {
        var repository = new BackendDashboardRepository(new StubApiClient(SampleInsights));

        var metrics = await repository.GetKpiMetricsAsync();

        var revenue = Assert.Single(metrics, m => m.Id == "kpi-revenue");
        Assert.Equal(TrendDirection.Up, revenue.TrendDirection);
        Assert.Equal(18.50, revenue.TrendPercentage);

        var bookings = Assert.Single(metrics, m => m.Id == "kpi-bookings");
        Assert.Equal("42", bookings.Value);

        var clients = Assert.Single(metrics, m => m.Id == "kpi-clients");
        Assert.Equal("18", clients.Value); // 6 new + 12 returning
    }

    [Fact]
    public async Task GetKpiMetricsAsync_NegativeGrowthRate_MapsToDownTrend()
    {
        var insights = SampleInsights with { Revenue = SampleInsights.Revenue with { GrowthRate = -5.00m } };
        var repository = new BackendDashboardRepository(new StubApiClient(insights));

        var metrics = await repository.GetKpiMetricsAsync();

        Assert.Equal(TrendDirection.Down, metrics.Single(m => m.Id == "kpi-revenue").TrendDirection);
    }

    [Fact]
    public async Task GetKpiMetricsAsync_BookingsAndCustomers_NeverFabricateATrend()
    {
        // The backend does not expose a period-over-period comparison for bookings/customers today -
        // this must stay Flat/0%, not a fabricated number, even though revenue has a real growth rate.
        var repository = new BackendDashboardRepository(new StubApiClient(SampleInsights));

        var metrics = await repository.GetKpiMetricsAsync();

        Assert.Equal(TrendDirection.Flat, metrics.Single(m => m.Id == "kpi-bookings").TrendDirection);
        Assert.Equal(0, metrics.Single(m => m.Id == "kpi-bookings").TrendPercentage);
        Assert.Equal(TrendDirection.Flat, metrics.Single(m => m.Id == "kpi-clients").TrendDirection);
    }

    [Fact]
    public async Task GetRecentActivityAsync_SurfacesEachRecommendationAsAnActivityEntry()
    {
        var repository = new BackendDashboardRepository(new StubApiClient(SampleInsights));

        var activity = await repository.GetRecentActivityAsync();

        Assert.Equal(2, activity.Count);
        Assert.Contains(activity, a => a.Description == "درآمد شما نسبت به دوره قبل رشد داشته است.");
        Assert.Contains(activity, a => a.Description == "پرمخاطب‌ترین خدمت این ماه: Haircut");
    }

    [Fact]
    public async Task GetRecentActivityAsync_NoRecommendations_ReturnsEmptyNotAnError()
    {
        var insights = SampleInsights with { Recommendations = [] };
        var repository = new BackendDashboardRepository(new StubApiClient(insights));

        var activity = await repository.GetRecentActivityAsync();

        Assert.Empty(activity);
    }

    [Fact]
    public async Task GetKpiMetricsAsync_ApiCallFails_ThrowsApiException()
    {
        var repository = new BackendDashboardRepository(new StubApiClient(failureStatusCode: 500, failureMessage: "Internal error"));

        await Assert.ThrowsAsync<ApiException>(() => repository.GetKpiMetricsAsync());
    }

    /// <summary>Minimal stub covering only what <see cref="BackendDashboardRepository"/> calls (<see cref="GetAsync{TResponse}"/>) - the other three <see cref="IApiClient"/> members throw if ever hit, since this repository never calls them.</summary>
    private sealed class StubApiClient : IApiClient
    {
        private readonly DashboardInsightsResponse? _insights;
        private readonly int? _failureStatusCode;
        private readonly string? _failureMessage;

        public StubApiClient(DashboardInsightsResponse insights)
        {
            _insights = insights;
        }

        public StubApiClient(int failureStatusCode, string failureMessage)
        {
            _failureStatusCode = failureStatusCode;
            _failureMessage = failureMessage;
        }

        public Task<ApiResponse<TResponse>> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default)
        {
            if (_insights is not null)
            {
                return Task.FromResult(ApiResponseFactory.Success((TResponse)(object)_insights, 200));
            }

            return Task.FromResult(ApiResponseFactory.Failure<TResponse>(_failureStatusCode, _failureMessage!));
        }

        public Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendDashboardRepository never posts.");

        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendDashboardRepository never puts.");

        public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendDashboardRepository never deletes.");

        public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendDashboardRepository never patches.");

        public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendDashboardRepository never patches.");
    }
}
