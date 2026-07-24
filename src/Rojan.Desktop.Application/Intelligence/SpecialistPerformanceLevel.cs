namespace Rojan.Desktop.Application.Intelligence;

/// <summary>
/// Application-owned mirror of <see cref="Rojan.Desktop.Domain.Specialists.SpecialistPerformanceLevel"/> -
/// Presentation never binds to a Domain-shaped type, so anything it needs
/// gets an Application-owned equivalent, mapped explicitly by
/// <see cref="IntelligenceMapper"/>, same reasoning <c>AI.InsightSeverity</c>/
/// <c>Specialists.SpecialistStatus</c> already establish.
/// </summary>
public enum SpecialistPerformanceLevel
{
    Underperforming,
    NeedsImprovement,
    Good,
    Excellent,
}
