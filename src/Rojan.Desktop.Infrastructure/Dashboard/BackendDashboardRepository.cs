using System.Globalization;
using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Domain.Dashboard;

namespace Rojan.Desktop.Infrastructure.Dashboard;

/// <summary>
/// Owner App Backend Integration: the real, backend-connected
/// <see cref="IDashboardRepository"/> - replaces <see cref="FakeDashboardRepository"/>
/// in <c>ServiceCollectionExtensions.AddInfrastructure</c> (which stays in
/// the codebase, unreferenced, matching this repo's own Sprint 6
/// convention). Calls <c>GET /api/v1/dashboard/insights</c> through the
/// normal <see cref="IApiClient"/> pipeline (unlike login/refresh - see
/// <c>Infrastructure.Api.AuthBootstrapHttpClient</c>'s own doc comment for
/// why those two bypass it - a dashboard read is an ordinary authenticated
/// GET with no risk of the 401-retry-recursion problem that only applies
/// to the refresh call itself). No <c>salonId</c> is ever sent - the
/// backend resolves the caller's salon from the bearer token alone.
///
/// Maps the real response onto the *existing* <see cref="KpiMetric"/>/
/// <see cref="ActivityEntry"/> shapes rather than introducing new domain
/// types, so no other layer (ViewModels, existing tests) needs to change
/// for real numbers to start flowing. Two honesty notes on the mapping:
/// <list type="bullet">
/// <item>Only <c>revenue.growthRate</c> has a real trend the backend
/// computes - bookings/customers KPIs below show <see cref="TrendDirection.Flat"/>/0%
/// rather than a fabricated trend, since the backend does not expose a
/// period-over-period comparison for those two today.</item>
/// <item>The backend's AI <c>recommendations</c> have no dedicated UI
/// concept in this app yet - they are surfaced here as
/// <see cref="ActivityEntry"/> items (real content, timestamped "now" since
/// a recommendation is a live computed insight, not an event with its own
/// occurred-at time) as the closest existing fit, pending a real
/// recommendations UI in a future phase.</item>
/// </list>
/// </summary>
public sealed class BackendDashboardRepository(IApiClient apiClient) : IDashboardRepository
{
    private const string InsightsPath = "/api/v1/dashboard/insights";

    public async Task<IReadOnlyList<KpiMetric>> GetKpiMetricsAsync(CancellationToken cancellationToken = default)
    {
        var insights = await FetchInsightsAsync(cancellationToken).ConfigureAwait(false);

        return
        [
            new KpiMetric(
                "kpi-revenue",
                "درآمد (ماه جاری)",
                FormatToman(insights.Revenue.Month),
                TrendDirectionOf(insights.Revenue.GrowthRate),
                (double)insights.Revenue.GrowthRate),
            new KpiMetric(
                "kpi-bookings",
                "مجموع نوبت‌ها",
                insights.Bookings.Total.ToString(CultureInfo.InvariantCulture),
                TrendDirection.Flat,
                0),
            new KpiMetric(
                "kpi-clients",
                "مشتریان فعال",
                (insights.Customers.NewCustomers + insights.Customers.ReturningCustomers).ToString(CultureInfo.InvariantCulture),
                TrendDirection.Flat,
                0),
        ];
    }

    public async Task<IReadOnlyList<ActivityEntry>> GetRecentActivityAsync(CancellationToken cancellationToken = default)
    {
        var insights = await FetchInsightsAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.Now;

        return insights.Recommendations
            .Select((recommendation, index) => new ActivityEntry($"recommendation-{index}", recommendation.Message, now))
            .ToList();
    }

    private async Task<DashboardInsightsResponse> FetchInsightsAsync(CancellationToken cancellationToken)
    {
        var response = await apiClient.GetAsync<DashboardInsightsResponse>(InsightsPath, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load dashboard insights (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data;
    }

    private static TrendDirection TrendDirectionOf(decimal growthRate) => growthRate switch
    {
        > 0 => TrendDirection.Up,
        < 0 => TrendDirection.Down,
        _ => TrendDirection.Flat,
    };

    private static string FormatToman(decimal amount) => $"{amount.ToString("N0", CultureInfo.InvariantCulture)} تومان";
}
