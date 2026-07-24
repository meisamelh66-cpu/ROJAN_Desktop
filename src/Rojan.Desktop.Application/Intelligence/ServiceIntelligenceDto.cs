namespace Rojan.Desktop.Application.Intelligence;

/// <summary>
/// A catalog service's calculated popularity intelligence - the raw
/// booking counts behind the score are included alongside the
/// <see cref="Domain.Services.ServicePopularityCalculator"/> results so a
/// future UI can explain a score, not just display it. Composed fresh on
/// every <see cref="IIntelligenceEngine.GetServiceIntelligenceAsync"/>
/// call, never persisted - same "calculated, not stored" reasoning
/// <c>Customers.CustomerInsightsDto</c> already establishes.
/// </summary>
public sealed record ServiceIntelligenceDto(
    string ServiceId,
    string ServiceName,
    int PopularityScore,
    ServicePopularityLevel PopularityLevel,
    ServiceRecommendationSignal RecommendationSignal,
    int CompletedBookingCount,
    int UpcomingBookingCount);
