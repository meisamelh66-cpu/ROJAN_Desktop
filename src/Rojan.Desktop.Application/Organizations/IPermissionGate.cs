namespace Rojan.Desktop.Application.Organizations;

/// <summary>
/// Phase 22A: Enterprise Context Migration. The single enforcement point
/// every module's mutating command service (Create/Update/Delete/Export)
/// now calls through - see each module's <c>*PermissionGate</c> decorator
/// (e.g. <c>Customers.CustomerCommandServicePermissionGate</c>) for where
/// this is actually invoked. <see cref="Ensure"/> throws rather than
/// returning a bool specifically so a call site cannot accidentally
/// ignore the result and execute anyway - "unauthorized operations must
/// never execute" is enforced by the type system, not by convention.
/// </summary>
public interface IPermissionGate
{
    /// <summary>Throws <see cref="UnauthorizedOperationException"/> if the current session's role does not have <paramref name="requiredPermission"/>.</summary>
    public void Ensure(Permission requiredPermission);
}
