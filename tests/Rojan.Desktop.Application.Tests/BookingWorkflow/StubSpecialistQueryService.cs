using Rojan.Desktop.Application.Specialists;

namespace Rojan.Desktop.Application.Tests.BookingWorkflow;

/// <summary>Minimal <see cref="ISpecialistQueryService"/> test double - only <see cref="GetSpecialistsAsync"/> is exercised by <see cref="BookingWorkflowServiceTests"/>, so <see cref="SearchSpecialistsAsync"/> just delegates to it.</summary>
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
        Task.FromResult(_specialists);
}
