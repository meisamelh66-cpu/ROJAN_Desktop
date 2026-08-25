namespace Rojan.Desktop.Domain.Services;

/// <summary>
/// Repository abstraction for the service catalog. Domain defines the
/// contract; Infrastructure provides the concrete implementation (a
/// fake/in-memory one for now - Phase 13 explicitly has no backend
/// integration yet, same as every other vertical slice in this app).
///
/// Service Catalog Management: <see cref="CreateServiceAsync"/>/<see cref="UpdateServiceAsync"/>/
/// <see cref="DeactivateServiceAsync"/> add real catalog authoring against
/// ROJAN_Backend's already-existing <c>ServiceController</c> endpoints (see
/// <c>BackendServiceRepository</c>'s own doc comment) - Backend remains the
/// sole authority for every field value and every activate/deactivate
/// decision; this repository only submits a request and returns the
/// confirmed response, never validates or decides locally. <see cref="GetCategoriesAsync"/>
/// exists solely so a Create-Service caller has real category ids to choose
/// from - a service only resolves through its owning category on
/// ROJAN_Backend, it has no independent existence.
/// </summary>
public interface IServiceRepository
{
    public Task<IReadOnlyList<Service>> GetServicesAsync(CancellationToken cancellationToken = default);

    public Task<Service?> GetServiceByIdAsync(string serviceId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<SpecialistService>> GetAssignedSpecialistsAsync(string serviceId, CancellationToken cancellationToken = default);

    public Task<SpecialistService> AssignSpecialistAsync(SpecialistService assignment, CancellationToken cancellationToken = default);

    public Task UnassignSpecialistAsync(string serviceId, string assignmentId, CancellationToken cancellationToken = default);

    /// <summary>Every real category for the current salon, needed to populate a Create-Service category picker.</summary>
    public Task<IReadOnlyList<ServiceCategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary><paramref name="service"/>.<see cref="Service.CategoryId"/> selects which category the new service is created under; <see cref="Service.Id"/> is ignored - Backend generates the real id.</summary>
    public Task<Service> CreateServiceAsync(Service service, CancellationToken cancellationToken = default);

    /// <summary>A full replacement of the editable fields (name/description/duration/price) - <paramref name="service"/>.<see cref="Service.CategoryId"/> is used only for routing, never sent as an editable field (ROJAN_Backend's update endpoint has no field to move a service between categories).</summary>
    public Task<Service> UpdateServiceAsync(Service service, CancellationToken cancellationToken = default);

    /// <summary>The only activate/deactivate operation ROJAN_Backend actually exposes for a service - there is no reactivate endpoint (see <c>BackendServiceRepository.DeactivateServiceAsync</c>'s own doc comment).</summary>
    public Task DeactivateServiceAsync(string categoryId, string serviceId, CancellationToken cancellationToken = default);
}
