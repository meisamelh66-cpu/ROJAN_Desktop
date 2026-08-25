namespace Rojan.Desktop.Application.Services;

/// <summary>
/// Write use cases for the service catalog. Service Catalog Management:
/// <see cref="CreateServiceAsync"/>/<see cref="UpdateServiceAsync"/>/<see cref="DeactivateServiceAsync"/>
/// add real catalog authoring against ROJAN_Backend's already-existing
/// <c>ServiceController</c> endpoints - Backend remains the sole authority
/// for every field value and every activate/deactivate decision; this
/// service only submits a request and returns the confirmed response,
/// never validates or decides locally. There is deliberately no "activate"
/// method - ROJAN_Backend has no endpoint to reverse a deactivation (see
/// <c>Infrastructure.Services.BackendServiceRepository.DeactivateServiceAsync</c>'s
/// own doc comment); adding one here would offer a control that could
/// never actually work.
/// </summary>
public interface IServiceCommandService
{
    public Task<AssignedSpecialistDto> AssignSpecialistAsync(string serviceId, string specialistName, CancellationToken cancellationToken = default);

    public Task UnassignSpecialistAsync(string serviceId, string assignmentId, CancellationToken cancellationToken = default);

    public Task<ServiceDto> CreateServiceAsync(CreateServiceRequest request, CancellationToken cancellationToken = default);

    public Task<ServiceDto> UpdateServiceAsync(UpdateServiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>The only activate/deactivate operation available - see this interface's own doc comment for why there is no reactivate counterpart.</summary>
    public Task DeactivateServiceAsync(string serviceId, CancellationToken cancellationToken = default);
}
