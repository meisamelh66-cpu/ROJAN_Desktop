using Rojan.Desktop.Application.Services;
using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Application.Specialists;

/// <summary>
/// Default <see cref="ISpecialistProfileQueryService"/> implementation -
/// fetches the specialist plus their skills from
/// <see cref="DomainSpecialists.ISpecialistRepository"/> and assembles
/// the aggregate <see cref="SpecialistProfileDto"/>.
///
/// Specialist-Service Assignment: also depends on
/// <see cref="IServiceQueryService"/> - <see cref="DomainSpecialists.ISpecialistRepository.GetAssignedServiceIdsAsync"/>
/// (a thin mirror of ROJAN_Backend's own id-only response) returns ids
/// only, so this is the layer that resolves each id to the real service
/// name against the catalog, the same "read across verticals to enrich a
/// display" shape <c>Application.Intelligence.IntelligenceEngine</c>
/// already establishes for this app - not a new architectural pattern. A
/// service id with no catalog match (e.g. deleted after being assigned)
/// falls back to the id itself rather than dropping the row or throwing -
/// the assignment is still real, only its display name is stale.
/// </summary>
public sealed class SpecialistProfileQueryService : ISpecialistProfileQueryService
{
    private readonly DomainSpecialists.ISpecialistRepository _repository;
    private readonly IServiceQueryService _serviceQueryService;

    public SpecialistProfileQueryService(DomainSpecialists.ISpecialistRepository repository, IServiceQueryService serviceQueryService)
    {
        _repository = repository;
        _serviceQueryService = serviceQueryService;
    }

    public async Task<SpecialistProfileDto> GetProfileAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        var specialist = await _repository.GetSpecialistByIdAsync(specialistId, cancellationToken).ConfigureAwait(true);
        if (specialist is null)
        {
            throw new InvalidOperationException($"Specialist '{specialistId}' was not found.");
        }

        var skills = await _repository.GetSkillsAsync(specialistId, cancellationToken).ConfigureAwait(true);
        var assignedServices = await BuildAssignedServicesAsync(specialistId, cancellationToken).ConfigureAwait(true);

        return new SpecialistProfileDto(
            SpecialistMapper.MapSpecialist(specialist),
            skills.Select(SpecialistMapper.MapSkill).ToList(),
            assignedServices);
    }

    private async Task<IReadOnlyList<AssignedServiceDto>> BuildAssignedServicesAsync(string specialistId, CancellationToken cancellationToken)
    {
        var assignedServiceIds = await _repository.GetAssignedServiceIdsAsync(specialistId, cancellationToken).ConfigureAwait(true);
        if (assignedServiceIds.Count == 0)
        {
            return [];
        }

        var catalog = await _serviceQueryService.GetServicesAsync(cancellationToken).ConfigureAwait(true);
        var namesById = catalog.ToDictionary(service => service.Id, service => service.Name);

        return assignedServiceIds
            .Select(serviceId => new AssignedServiceDto(serviceId, namesById.GetValueOrDefault(serviceId, serviceId)))
            .ToList();
    }
}
