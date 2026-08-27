using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Services;

/// <summary>
/// Phase 22A: Enterprise Context Migration - same "wrap the real service
/// with permission enforcement" pattern as
/// <c>Customers.CustomerCommandServicePermissionGate</c>.
///
/// Remediation Phase 1 (RBAC Backend Authority Migration): migrated off
/// the legacy <see cref="IPermissionGate"/>/<c>RolePermissions</c> check
/// entirely, same shape <c>Bookings.BookingCommandServicePermissionGate</c>
/// already established. <see cref="CreateServiceAsync"/>/<see cref="UpdateServiceAsync"/>
/// call the real <c>POST/PUT /categories/{id}/services</c> endpoints,
/// which ROJAN_Backend's own <c>CreateServiceUseCase</c>/<c>UpdateServiceUseCase</c>/
/// <c>DeactivateServiceUseCase</c> all gate on <c>Permission.MANAGE_CATALOG</c>
/// alone (verified directly against ROJAN_Backend source this migration).
/// <see cref="AssignSpecialistAsync"/>/<see cref="UnassignSpecialistAsync"/>
/// are unreachable in practice either way - the wrapped
/// <c>BackendServiceRepository</c> always throws <c>NotSupportedException</c>
/// for both (ROJAN_Backend has no specialist-to-service assignment concept
/// on the Service side, per that repository's own doc comment) - gated
/// here on the same <c>MANAGE_CATALOG</c> string purely for consistency
/// with the other two Service actions, not because any real request ever
/// reaches the backend through them.
/// </summary>
public sealed class ServiceCommandServicePermissionGate : IServiceCommandService
{
    private const string ManageCatalog = "MANAGE_CATALOG";

    private readonly IServiceCommandService _inner;
    private readonly IBackendPermissionGate _backendPermissionGate;

    public ServiceCommandServicePermissionGate(IServiceCommandService inner, IBackendPermissionGate backendPermissionGate)
    {
        _inner = inner;
        _backendPermissionGate = backendPermissionGate;
    }

    public Task<AssignedSpecialistDto> AssignSpecialistAsync(string serviceId, string specialistName, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageCatalog);
        return _inner.AssignSpecialistAsync(serviceId, specialistName, cancellationToken);
    }

    public Task UnassignSpecialistAsync(string serviceId, string assignmentId, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageCatalog);
        return _inner.UnassignSpecialistAsync(serviceId, assignmentId, cancellationToken);
    }

    public Task<ServiceDto> CreateServiceAsync(CreateServiceRequest request, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageCatalog);
        return _inner.CreateServiceAsync(request, cancellationToken);
    }

    public Task<ServiceDto> UpdateServiceAsync(UpdateServiceRequest request, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageCatalog);
        return _inner.UpdateServiceAsync(request, cancellationToken);
    }
}
