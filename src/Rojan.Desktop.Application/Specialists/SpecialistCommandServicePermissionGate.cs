using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Specialists;

/// <summary>
/// Phase 22A: Enterprise Context Migration - same "wrap the real service
/// with permission enforcement" pattern as
/// <c>Customers.CustomerCommandServicePermissionGate</c>.
///
/// Remediation Phase 1 (RBAC Backend Authority Migration), High Priority
/// (Task 3): migrated off the legacy <see cref="IPermissionGate"/>/
/// <c>RolePermissions</c> check entirely. ROJAN_Backend's own
/// <c>CreateSpecialistUseCase</c>/<c>UpdateSpecialistUseCase</c>/
/// <c>DeactivateSpecialistUseCase</c> all gate on <c>Permission.MANAGE_STAFF</c>
/// alone; <c>SpecialistServiceUseCases</c> (backing
/// <see cref="AssignServiceAsync"/>/<see cref="RemoveServiceAssignmentAsync"/>)
/// allows either <c>MANAGE_STAFF</c> or the specialist acting on their own
/// record (<c>MANAGE_SCHEDULE_OWN</c>) - all verified directly against
/// ROJAN_Backend source this migration. This gate deliberately checks only
/// <c>MANAGE_STAFF</c> for every method, including those two - narrower
/// than the backend's own own-record allowance, not wider - because
/// Desktop's session model has no "is this the caller's own specialist
/// record" signal to check locally, and this task's own explicit priority
/// ("prevent Specialist receiving broader permissions locally") means the
/// safe default here is stricter, not more permissive. A future change
/// could special-case those two methods once Desktop's session exposes
/// the caller's own specialistId - out of this migration's scope.
/// <see cref="AddSkillAsync"/>/<see cref="RemoveSkillAsync"/> are
/// unreachable in practice either way - the wrapped
/// <c>BackendSpecialistRepository</c> always throws
/// <c>NotSupportedException</c> for both (ROJAN_Backend has no
/// specialist-skill concept, per that repository's own doc comment) -
/// gated here on <c>MANAGE_STAFF</c> purely for consistency, not because
/// any real request ever reaches the backend through them.
/// </summary>
public sealed class SpecialistCommandServicePermissionGate : ISpecialistCommandService
{
    private const string ManageStaff = "MANAGE_STAFF";

    private readonly ISpecialistCommandService _inner;
    private readonly IBackendPermissionGate _backendPermissionGate;

    public SpecialistCommandServicePermissionGate(ISpecialistCommandService inner, IBackendPermissionGate backendPermissionGate)
    {
        _inner = inner;
        _backendPermissionGate = backendPermissionGate;
    }

    public Task<SpecialistDto> CreateSpecialistAsync(CreateSpecialistRequest request, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageStaff);
        return _inner.CreateSpecialistAsync(request, cancellationToken);
    }

    public Task<SpecialistDto> UpdateSpecialistAsync(UpdateSpecialistRequest request, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageStaff);
        return _inner.UpdateSpecialistAsync(request, cancellationToken);
    }

    public Task<SpecialistSkillDto> AddSkillAsync(string specialistId, string name, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageStaff);
        return _inner.AddSkillAsync(specialistId, name, cancellationToken);
    }

    public Task RemoveSkillAsync(string specialistId, string skillId, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageStaff);
        return _inner.RemoveSkillAsync(specialistId, skillId, cancellationToken);
    }

    public Task AssignServiceAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageStaff);
        return _inner.AssignServiceAsync(specialistId, serviceId, cancellationToken);
    }

    public Task RemoveServiceAssignmentAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default)
    {
        _backendPermissionGate.EnsureBackend(ManageStaff);
        return _inner.RemoveServiceAssignmentAsync(specialistId, serviceId, cancellationToken);
    }
}
