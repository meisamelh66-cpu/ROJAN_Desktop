namespace Rojan.Desktop.Application.Organizations;

/// <summary>Default <see cref="IBackendPermissionGate"/>. See that interface's own doc comment.</summary>
public sealed class BackendPermissionGate(IEnterpriseContext enterpriseContext) : IBackendPermissionGate
{
    public void EnsureBackend(string requiredPermission)
    {
        if (!enterpriseContext.BackendPermissions.Contains(requiredPermission))
        {
            throw new UnauthorizedOperationException($"The current session does not have the backend permission '{requiredPermission}' required for this operation.");
        }
    }
}
