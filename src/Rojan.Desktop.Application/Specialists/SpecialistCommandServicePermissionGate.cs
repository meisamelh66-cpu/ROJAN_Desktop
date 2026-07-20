using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Specialists;

/// <summary>Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Every method requires <see cref="Permission.SpecialistEdit"/>.</summary>
public sealed class SpecialistCommandServicePermissionGate : ISpecialistCommandService
{
    private readonly ISpecialistCommandService _inner;
    private readonly IPermissionGate _permissionGate;

    public SpecialistCommandServicePermissionGate(ISpecialistCommandService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<SpecialistDto> CreateSpecialistAsync(CreateSpecialistRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.SpecialistEdit);
        return _inner.CreateSpecialistAsync(request, cancellationToken);
    }

    public Task<SpecialistDto> UpdateSpecialistAsync(UpdateSpecialistRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.SpecialistEdit);
        return _inner.UpdateSpecialistAsync(request, cancellationToken);
    }

    public Task<SpecialistSkillDto> AddSkillAsync(string specialistId, string name, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.SpecialistEdit);
        return _inner.AddSkillAsync(specialistId, name, cancellationToken);
    }

    public Task RemoveSkillAsync(string specialistId, string skillId, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.SpecialistEdit);
        return _inner.RemoveSkillAsync(specialistId, skillId, cancellationToken);
    }
}
