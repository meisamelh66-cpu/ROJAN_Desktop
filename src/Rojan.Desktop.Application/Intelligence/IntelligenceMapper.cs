using DomainServices = Rojan.Desktop.Domain.Services;
using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Application.Intelligence;

/// <summary>Domain&lt;-&gt;Application mapping for the Intelligence vertical slice - internal, only <see cref="IntelligenceEngine"/> calls it, matching every other module's Mapper convention (<c>Customers.CustomerMapper</c>, <c>AI.AIMapper</c>).</summary>
internal static class IntelligenceMapper
{
    public static SpecialistPerformanceLevel MapPerformanceLevel(DomainSpecialists.SpecialistPerformanceLevel level) => level switch
    {
        DomainSpecialists.SpecialistPerformanceLevel.Underperforming => SpecialistPerformanceLevel.Underperforming,
        DomainSpecialists.SpecialistPerformanceLevel.NeedsImprovement => SpecialistPerformanceLevel.NeedsImprovement,
        DomainSpecialists.SpecialistPerformanceLevel.Good => SpecialistPerformanceLevel.Good,
        DomainSpecialists.SpecialistPerformanceLevel.Excellent => SpecialistPerformanceLevel.Excellent,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown domain specialist performance level."),
    };

    public static SpecialistRecommendationSignal MapRecommendationSignal(DomainSpecialists.SpecialistRecommendationSignal signal) => signal switch
    {
        DomainSpecialists.SpecialistRecommendationSignal.Attention => SpecialistRecommendationSignal.Attention,
        DomainSpecialists.SpecialistRecommendationSignal.Monitor => SpecialistRecommendationSignal.Monitor,
        DomainSpecialists.SpecialistRecommendationSignal.Maintain => SpecialistRecommendationSignal.Maintain,
        DomainSpecialists.SpecialistRecommendationSignal.Promote => SpecialistRecommendationSignal.Promote,
        _ => throw new ArgumentOutOfRangeException(nameof(signal), signal, "Unknown domain specialist recommendation signal."),
    };

    public static ServicePopularityLevel MapPopularityLevel(DomainServices.ServicePopularityLevel level) => level switch
    {
        DomainServices.ServicePopularityLevel.LowDemand => ServicePopularityLevel.LowDemand,
        DomainServices.ServicePopularityLevel.Standard => ServicePopularityLevel.Standard,
        DomainServices.ServicePopularityLevel.Trending => ServicePopularityLevel.Trending,
        DomainServices.ServicePopularityLevel.Popular => ServicePopularityLevel.Popular,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown domain service popularity level."),
    };

    public static ServiceRecommendationSignal MapRecommendationSignal(DomainServices.ServiceRecommendationSignal signal) => signal switch
    {
        DomainServices.ServiceRecommendationSignal.Reconsider => ServiceRecommendationSignal.Reconsider,
        DomainServices.ServiceRecommendationSignal.Monitor => ServiceRecommendationSignal.Monitor,
        DomainServices.ServiceRecommendationSignal.Maintain => ServiceRecommendationSignal.Maintain,
        DomainServices.ServiceRecommendationSignal.Feature => ServiceRecommendationSignal.Feature,
        _ => throw new ArgumentOutOfRangeException(nameof(signal), signal, "Unknown domain service recommendation signal."),
    };
}
