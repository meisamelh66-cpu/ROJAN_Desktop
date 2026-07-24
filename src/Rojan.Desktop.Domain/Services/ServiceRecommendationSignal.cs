namespace Rojan.Desktop.Domain.Services;

/// <summary>
/// A calculated recommendation signal for a catalog service - whether the
/// business should feature this service more, keep things as they are,
/// keep an eye on it, or reconsider offering it. Purely a classification
/// derived by <see cref="ServicePopularityCalculator.ClassifySignal"/> from
/// <see cref="ServicePopularityLevel"/> and <see cref="ServiceStatus"/> - it
/// never mutates a service's actual status, that stays entirely
/// <see cref="ServiceRules"/>'s job.
/// </summary>
public enum ServiceRecommendationSignal
{
    Reconsider,
    Monitor,
    Maintain,
    Feature,
}
