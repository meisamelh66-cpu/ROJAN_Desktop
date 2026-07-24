namespace Rojan.Desktop.Application.Intelligence;

/// <summary>
/// A specialist's calculated performance intelligence - the raw booking
/// counts behind the score are included alongside the
/// <see cref="Domain.Specialists.SpecialistPerformanceCalculator"/> results
/// so a future UI can explain a score, not just display it. Composed fresh
/// on every <see cref="IIntelligenceEngine.GetSpecialistIntelligenceAsync"/>
/// call, never persisted - same "calculated, not stored" reasoning
/// <c>Customers.CustomerInsightsDto</c> already establishes.
/// </summary>
public sealed record SpecialistIntelligenceDto(
    string SpecialistId,
    string SpecialistName,
    int PerformanceScore,
    SpecialistPerformanceLevel PerformanceLevel,
    SpecialistRecommendationSignal RecommendationSignal,
    int CompletedBookingCount,
    int CancelledBookingCount,
    int NoShowBookingCount);
