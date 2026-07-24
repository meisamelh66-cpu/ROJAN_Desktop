namespace Rojan.Desktop.Domain.Services;

/// <summary>A service's calculated demand tier - see <see cref="ServicePopularityCalculator.ClassifyPopularity"/> for the score thresholds. Ordered low-to-high.</summary>
public enum ServicePopularityLevel
{
    LowDemand,
    Standard,
    Trending,
    Popular,
}
