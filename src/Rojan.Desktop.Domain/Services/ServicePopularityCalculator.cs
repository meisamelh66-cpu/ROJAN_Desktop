namespace Rojan.Desktop.Domain.Services;

/// <summary>
/// Service Intelligence (Sprint 5 Commit 5A): genuine Domain math behind a
/// catalog service's popularity score/level/recommendation signal - the
/// same "Domain business math, composed in Application" pattern
/// <c>HR.CommissionCalculator</c>/<c>AI.BusinessHealthCalculator</c>/
/// <c>Specialists.SpecialistPerformanceCalculator</c> already establish.
/// Every method here is pure and takes its inputs as parameters
/// (<see cref="ServicePopularityIndicators"/>, an already-known
/// <see cref="ServiceStatus"/>) rather than fetching them - Domain owns
/// the definition and the calculation, never the data source. Negative
/// counts are treated as zero rather than rejected, the same "degrade
/// gracefully, never throw on a degenerate input" convention
/// <c>AI.BusinessHealthCalculator.ComputeOverallScore</c> already follows
/// for a zero total weight.
/// </summary>
public static class ServicePopularityCalculator
{
    /// <summary>
    /// Completed bookings count twice as much as upcoming ones (a
    /// completed booking is proven demand; an upcoming one is only a
    /// forward-looking signal until it actually happens), clamped to
    /// [0, 100] so the result always reads as a percentage-like figure
    /// regardless of how many bookings a long-catalogued service
    /// accumulates.
    /// </summary>
    public static int ComputePopularityScore(ServicePopularityIndicators indicators)
    {
        var completed = Math.Max(0, indicators.CompletedBookingCount);
        var upcoming = Math.Max(0, indicators.UpcomingBookingCount);

        var score = (completed * 8) + (upcoming * 4);
        return Math.Clamp(score, 0, 100);
    }

    /// <summary>Popular (70+) -&gt; Trending (35+) -&gt; Standard (10+) -&gt; LowDemand (below 10), purely a function of <see cref="ComputePopularityScore"/>'s result.</summary>
    public static ServicePopularityLevel ClassifyPopularity(int score) => score switch
    {
        >= 70 => ServicePopularityLevel.Popular,
        >= 35 => ServicePopularityLevel.Trending,
        >= 10 => ServicePopularityLevel.Standard,
        _ => ServicePopularityLevel.LowDemand,
    };

    /// <summary>
    /// Combines the calculated <paramref name="level"/> with the service's
    /// actual <paramref name="status"/> (<see cref="ServiceRules"/> already
    /// owns what that status means/how it may change - this only reads
    /// it): a <see cref="ServiceStatus.Discontinued"/> service always needs
    /// Reconsideration regardless of how popular it once was, a
    /// <see cref="ServiceStatus.Seasonal"/> one is always worth Monitoring
    /// (its demand is expected to swing with the season), and only an
    /// Active service's signal is driven purely by <paramref name="level"/>.
    /// </summary>
    public static ServiceRecommendationSignal ClassifySignal(ServicePopularityLevel level, ServiceStatus status)
    {
        if (status == ServiceStatus.Discontinued)
        {
            return ServiceRecommendationSignal.Reconsider;
        }

        if (status == ServiceStatus.Seasonal)
        {
            return ServiceRecommendationSignal.Monitor;
        }

        return level switch
        {
            ServicePopularityLevel.Popular => ServiceRecommendationSignal.Feature,
            ServicePopularityLevel.Trending => ServiceRecommendationSignal.Maintain,
            ServicePopularityLevel.Standard => ServiceRecommendationSignal.Monitor,
            _ => ServiceRecommendationSignal.Reconsider,
        };
    }
}
