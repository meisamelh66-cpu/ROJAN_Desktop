namespace Rojan.Desktop.Application.Services;

/// <summary>
/// Write use cases for the service catalog. <see cref="AssignSpecialistAsync"/>/
/// <see cref="UnassignSpecialistAsync"/> are the original, free-text,
/// service-centric assignment model (Phase 13) - deliberately untouched by
/// Service Catalog Authoring below; see <c>Domain.Specialists.ISpecialistRepository</c>'s
/// own doc comment for the real, specialist-centric model that superseded
/// it for actual use. <see cref="CreateServiceAsync"/>/<see cref="UpdateServiceAsync"/>
/// close the "catalog authoring is a future concern" gap this interface's
/// doc comment used to describe.
/// </summary>
public interface IServiceCommandService
{
    public Task<AssignedSpecialistDto> AssignSpecialistAsync(string serviceId, string specialistName, CancellationToken cancellationToken = default);

    public Task UnassignSpecialistAsync(string serviceId, string assignmentId, CancellationToken cancellationToken = default);

    public Task<ServiceDto> CreateServiceAsync(CreateServiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Edits a service's fields and/or requests a status change - see <see cref="Domain.Services.IServiceRepository.UpdateServiceAsync"/>'s own doc comment for why deactivation is folded in here rather than a separate method, and for the Active-&gt;Discontinued-only scope.</summary>
    public Task<ServiceDto> UpdateServiceAsync(UpdateServiceRequest request, CancellationToken cancellationToken = default);
}
