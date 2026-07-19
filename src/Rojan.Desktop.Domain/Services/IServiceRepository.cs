namespace Rojan.Desktop.Domain.Services;

/// <summary>
/// Repository abstraction for the service catalog. Domain defines the
/// contract; Infrastructure provides the concrete implementation (a
/// fake/in-memory one for now - Phase 13 explicitly has no backend
/// integration yet, same as every other vertical slice in this app).
/// Deliberately read-plus-assignment only, not full CRUD: Phase 13's scope
/// is search/browse the catalog and manage specialist assignments, not
/// catalog authoring (no create/update-service commands were requested).
/// </summary>
public interface IServiceRepository
{
    public Task<IReadOnlyList<Service>> GetServicesAsync(CancellationToken cancellationToken = default);

    public Task<Service?> GetServiceByIdAsync(string serviceId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<SpecialistService>> GetAssignedSpecialistsAsync(string serviceId, CancellationToken cancellationToken = default);

    public Task<SpecialistService> AssignSpecialistAsync(SpecialistService assignment, CancellationToken cancellationToken = default);

    public Task UnassignSpecialistAsync(string serviceId, string assignmentId, CancellationToken cancellationToken = default);
}
