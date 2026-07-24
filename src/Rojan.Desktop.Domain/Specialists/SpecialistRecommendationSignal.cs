namespace Rojan.Desktop.Domain.Specialists;

/// <summary>
/// A calculated recommendation signal for a specialist - whether the
/// business should feature/promote this specialist more, keep things as
/// they are, keep an eye on them, or address a concern. Purely a
/// classification derived by <see cref="SpecialistPerformanceCalculator.ClassifySignal"/>
/// from <see cref="SpecialistPerformanceLevel"/> and <see cref="SpecialistStatus"/> -
/// it never mutates a specialist's actual status, that stays entirely
/// <see cref="SpecialistRules"/>'s job.
/// </summary>
public enum SpecialistRecommendationSignal
{
    Attention,
    Monitor,
    Maintain,
    Promote,
}
