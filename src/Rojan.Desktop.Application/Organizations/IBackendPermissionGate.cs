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

    /// <summary>
    /// Phase 3B Booking Permission Migration: throws <see cref="UnauthorizedOperationException"/>
    /// unless at least one of <paramref name="anyOfPermissions"/> is present in
    /// <see cref="IEnterpriseContext.BackendPermissions"/>. Still encodes no rule
    /// of its own - a caller decides which permissions are alternatives for its
    /// own action (e.g. <c>MANAGE_BOOKINGS</c> or <c>MANAGE_OWN_BOOKINGS</c> for
    /// booking management); this only checks membership against the set,
    /// exactly like <see cref="EnsureBackend"/>, just against more than one
    /// candidate string.
    /// </summary>
    public void EnsureBackendAny(params string[] anyOfPermissions);
}
