using Rojan.Desktop.Application.Intelligence;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

/// <summary>Minimal <see cref="IIntelligenceEngine"/> test double - only <see cref="GetSpecialistIntelligenceAsync"/> is exercised by the Specialists tests, so <see cref="GetServiceIntelligenceAsync"/> always returns an empty list.</summary>
internal sealed class StubIntelligenceEngine : IIntelligenceEngine
{
    public StubIntelligenceEngine(IReadOnlyList<SpecialistIntelligenceDto>? specialistIntelligence = null)
    {
        SpecialistIntelligence = specialistIntelligence ?? [];
    }

    /// <summary>Mutable between calls - lets a test change what the next <see cref="GetSpecialistIntelligenceAsync"/> call returns, to assert refresh behavior (a reload picks up new data, not a cached first result).</summary>
    public IReadOnlyList<SpecialistIntelligenceDto> SpecialistIntelligence { get; set; }

    public Task<IReadOnlyList<SpecialistIntelligenceDto>> GetSpecialistIntelligenceAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(SpecialistIntelligence);

    public Task<IReadOnlyList<ServiceIntelligenceDto>> GetServiceIntelligenceAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceIntelligenceDto>>([]);
}
