using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Services;

/// <summary>Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Every method requires <see cref="Permission.ServiceEdit"/>.</summary>
public sealed class ServiceCommandServicePermissionGate : IServiceCommandService
{
    private readonly IServiceCommandService _inner;
    private readonly IPermissionGate _permissionGate;

    public ServiceCommandServicePermissionGate(IServiceCommandService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<AssignedSpecialistDto> AssignSpecialistAsync(string serviceId, string specialistName, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.ServiceEdit);
        return _inner.AssignSpecialistAsync(serviceId, specialistName, cancellationToken);
    }

    public Task UnassignSpecialistAsync(string serviceId, string assignmentId, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.ServiceEdit);
        return _inner.UnassignSpecialistAsync(serviceId, assignmentId, cancellationToken);
    }
}
