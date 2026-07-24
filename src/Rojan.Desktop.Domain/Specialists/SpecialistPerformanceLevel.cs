namespace Rojan.Desktop.Domain.Specialists;

/// <summary>A specialist's calculated performance tier - see <see cref="SpecialistPerformanceCalculator.ClassifyPerformance"/> for the score thresholds. Ordered low-to-high.</summary>
public enum SpecialistPerformanceLevel
{
    Underperforming,
    NeedsImprovement,
    Good,
    Excellent,
}
