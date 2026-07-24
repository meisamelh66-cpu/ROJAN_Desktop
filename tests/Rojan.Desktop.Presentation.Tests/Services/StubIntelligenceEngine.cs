using Rojan.Desktop.Application.Intelligence;

namespace Rojan.Desktop.Presentation.Tests.Services;

/// <summary>Minimal <see cref="IIntelligenceEngine"/> test double - only <see cref="GetServiceIntelligenceAsync"/> is exercised by the Services tests, so <see cref="GetSpecialistIntelligenceAsync"/> always returns an empty list.</summary>
internal sealed class StubIntelligenceEngine : IIntelligenceEngine
{
    public StubIntelligenceEngine(IReadOnlyList<ServiceIntelligenceDto>? serviceIntelligence = null)
    {
        ServiceIntelligence = serviceIntelligence ?? [];
    }

    /// <summary>Mutable between calls - lets a test change what the next <see cref="GetServiceIntelligenceAsync"/> call returns, to assert refresh behavior (a reload picks up new data, not a cached first result).</summary>
    public IReadOnlyList<ServiceIntelligenceDto> ServiceIntelligence { get; set; }

    public Task<IReadOnlyList<SpecialistIntelligenceDto>> GetSpecialistIntelligenceAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SpecialistIntelligenceDto>>([]);

    public Task<IReadOnlyList<ServiceIntelligenceDto>> GetServiceIntelligenceAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(ServiceIntelligence);
}
