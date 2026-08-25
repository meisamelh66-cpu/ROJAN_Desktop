using Rojan.Desktop.Application.Specialists;

namespace Rojan.Desktop.Application.Tests.Intelligence;

/// <summary>Minimal <see cref="ISpecialistQueryService"/> test double - only <see cref="GetSpecialistsAsync"/> is exercised by <see cref="IntelligenceEngineTests"/>.</summary>
internal sealed class StubSpecialistQueryService : ISpecialistQueryService
{
    private readonly IReadOnlyList<SpecialistDto> _specialists;

    public StubSpecialistQueryService(IReadOnlyList<SpecialistDto> specialists)
    {
        _specialists = specialists;
    }

    public Task<IReadOnlyList<SpecialistDto>> GetSpecialistsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_specialists);

    public Task<IReadOnlyList<SpecialistDto>> SearchSpecialistsAsync(string searchText, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by IntelligenceEngineTests.");

    public Task<IReadOnlyList<SpecialistDto>> SearchSpecialistsAsync(SpecialistSearchFilter filter, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by IntelligenceEngineTests.");

    public Task<IReadOnlyList<string>> GetAssignedServiceIdsAsync(string specialistId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by IntelligenceEngineTests.");
}
