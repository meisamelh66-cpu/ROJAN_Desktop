namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// The response body <c>GET {ApiVersion.BasePath()}/dashboard/insights</c>
/// returns - matches ROJAN_Backend's <c>DashboardInsightsResponse</c>
/// field-for-field (see that project's <c>api/dashboard/DashboardDtos.kt</c>).
/// No `salonId` parameter is sent - the backend resolves the caller's salon
/// from the bearer token alone.
/// </summary>
public sealed record DashboardInsightsResponse(
    DashboardRevenueResponse Revenue,
    DashboardBookingCountsResponse Bookings,
    DashboardCustomerCountsResponse Customers,
    IReadOnlyList<DashboardServiceInsightResponse> Services,
    IReadOnlyList<DashboardRecommendationResponse> Recommendations);

public sealed record DashboardRevenueResponse(decimal Today, decimal Month, decimal GrowthRate);

public sealed record DashboardBookingCountsResponse(long Total, long Completed, long Cancelled);

public sealed record DashboardCustomerCountsResponse(int NewCustomers, int ReturningCustomers);

public sealed record DashboardServiceInsightResponse(string Name, long Bookings, decimal Revenue);

/// <summary><see cref="Priority"/> is one of <c>LOW</c>/<c>MEDIUM</c>/<c>HIGH</c>; <see cref="Type"/> is one of the <c>RecommendationType</c> enum names the backend's rule-based engine emits (e.g. <c>REVENUE_GROWTH</c>). Both stay plain strings here rather than a desktop-side enum - a new backend rule type must never fail to deserialize on an older client.</summary>
public sealed record DashboardRecommendationResponse(string Type, string Priority, string Message);
