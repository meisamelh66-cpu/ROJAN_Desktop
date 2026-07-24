namespace Rojan.Desktop.Application.Intelligence;

/// <summary>
/// Application-owned mirror of <see cref="Rojan.Desktop.Domain.Specialists.SpecialistRecommendationSignal"/> -
/// see <see cref="SpecialistPerformanceLevel"/>'s doc comment for why
/// Application never exposes the Domain enum directly.
/// </summary>
public enum SpecialistRecommendationSignal
{
    Attention,
    Monitor,
    Maintain,
    Promote,
}
