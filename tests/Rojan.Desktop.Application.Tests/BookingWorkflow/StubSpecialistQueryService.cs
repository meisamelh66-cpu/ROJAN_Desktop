using Rojan.Desktop.Application.Specialists;

namespace Rojan.Desktop.Application.Tests.BookingWorkflow;

/// <summary>
/// Minimal <see cref="ISpecialistQueryService"/> test double - only
/// <see cref="GetSpecialistsAsync"/> is exercised by
/// <see cref="BookingWorkflowServiceTests"/>, so <see cref="SearchSpecialistsAsync(string, CancellationToken)"/>
/// just delegates to it. Booking Eligibility Filter:
/// <see cref="GetAssignedServiceIdsAsync"/> is configurable per specialist
/// id via the constructor's optional dictionary (an unconfigured
/// specialist returns an empty list, the correct "unrestricted" default)
/// so <see cref="BookingWorkflowServiceTests"/> can exercise both the
/// empty-means-unrestricted and the real-restriction cases.
/// </summary>
internal sealed class StubSpecialistQueryService : ISpecialistQueryService
{
    private readonly IReadOnlyList<SpecialistDto> _specialists;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _assignedServiceIdsBySpecialistId;

    public StubSpecialistQueryService(
        IReadOnlyList<SpecialistDto> specialists,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? assignedServiceIdsBySpecialistId = null)
    {
        _specialists = specialists;
        _assignedServiceIdsBySpecialistId = assignedServiceIdsBySpecialistId ?? new Dictionary<string, IReadOnlyList<string>>();
    }

    public Task<IReadOnlyList<SpecialistDto>> GetSpecialistsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_specialists);

    public Task<IReadOnlyList<SpecialistDto>> SearchSpecialistsAsync(string searchText, CancellationToken cancellationToken = default) =>
        Task.FromResult(_specialists);

    public Task<IReadOnlyList<SpecialistDto>> SearchSpecialistsAsync(SpecialistSearchFilter filter, CancellationToken cancellationToken = default) =>
        Task.FromResult(_specialists);

    public Task<IReadOnlyList<string>> GetAssignedServiceIdsAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_assignedServiceIdsBySpecialistId.GetValueOrDefault(specialistId, []));
}
