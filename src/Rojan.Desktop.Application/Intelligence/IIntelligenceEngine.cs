namespace Rojan.Desktop.Application.Intelligence;

/// <summary>
/// Application Intelligence (Sprint 5 Commit 5B): orchestrates
/// <c>Domain.Specialists.SpecialistPerformanceCalculator</c>/
/// <c>Domain.Services.ServicePopularityCalculator</c> (Sprint 5 Commit 5A's
/// Domain foundation) over real specialist/service/booking data. This
/// interface only composes already-known Domain math over already-scoped
/// Application query services (<see cref="Specialists.ISpecialistQueryService"/>,
/// <see cref="Services.IServiceQueryService"/>, <see cref="Bookings.IBookingQueryService"/>) -
/// same "Application composes over a sibling module" shape
/// <c>Customers.CustomerProfileQueryService</c>/<c>AI.InsightEngine</c>
/// already establish. Every score/level/signal is still calculated
/// entirely by Domain; this layer only counts bookings and hands them to
/// Domain as already-known primitives - no business rule is duplicated
/// here.
/// </summary>
public interface IIntelligenceEngine
{
    /// <summary>Every specialist's calculated performance intelligence, ordered by <see cref="SpecialistIntelligenceDto.PerformanceScore"/> descending (best performers first) with a deterministic tie-break by <see cref="SpecialistIntelligenceDto.SpecialistName"/> (ordinal) - never returns in an unstable or non-reproducible order. Returns an empty list if there are no specialists.</summary>
    public Task<IReadOnlyList<SpecialistIntelligenceDto>> GetSpecialistIntelligenceAsync(CancellationToken cancellationToken = default);

    /// <summary>Every catalog service's calculated popularity intelligence, ordered by <see cref="ServiceIntelligenceDto.PopularityScore"/> descending (most in-demand first) with a deterministic tie-break by <see cref="ServiceIntelligenceDto.ServiceName"/> (ordinal) - never returns in an unstable or non-reproducible order. Returns an empty list if there are no services.</summary>
    public Task<IReadOnlyList<ServiceIntelligenceDto>> GetServiceIntelligenceAsync(CancellationToken cancellationToken = default);
}
