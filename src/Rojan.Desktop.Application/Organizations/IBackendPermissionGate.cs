namespace Rojan.Desktop.Application.Organizations;

/// <summary>
/// Phase 3A Permission Consumer Adapter: the enforcement seam for
/// backend-sourced permission strings - the sibling <see cref="IPermissionGate"/>
/// already is for <c>RolePermissions</c>-sourced ones. Encodes no rule of
/// its own: the backend (<c>SalonPermissionResolver</c>) already decided
/// what's in the set (see <see cref="IEnterpriseContext.BackendPermissions"/>);
/// this only checks membership. Not a replacement for <see cref="IPermissionGate"/> -
/// both exist side by side until a future phase migrates specific call
/// sites (see ROJAN_Phase3_Permission_Migration_Plan_v1.md).
/// </summary>
public interface IBackendPermissionGate
{
    /// <summary>Throws <see cref="UnauthorizedOperationException"/> if <paramref name="requiredPermission"/> is not present in <see cref="IEnterpriseContext.BackendPermissions"/>.</summary>
    public void EnsureBackend(string requiredPermission);
}
