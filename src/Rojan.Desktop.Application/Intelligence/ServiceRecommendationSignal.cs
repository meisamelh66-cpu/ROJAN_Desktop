namespace Rojan.Desktop.Application.Intelligence;

/// <summary>
/// Application-owned mirror of <see cref="Rojan.Desktop.Domain.Services.ServiceRecommendationSignal"/> -
/// see <see cref="SpecialistPerformanceLevel"/>'s doc comment for why
/// Application never exposes the Domain enum directly.
/// </summary>
public enum ServiceRecommendationSignal
{
    Reconsider,
    Monitor,
    Maintain,
    Feature,
}
