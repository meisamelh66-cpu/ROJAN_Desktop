namespace Rojan.Desktop.Application.Organizations;

public sealed class PermissionGate : IPermissionGate
{
    private readonly IPermissionEngine _permissionEngine;
    private readonly IEnterpriseContext _enterpriseContext;

    public PermissionGate(IPermissionEngine permissionEngine, IEnterpriseContext enterpriseContext)
    {
        _permissionEngine = permissionEngine;
        _enterpriseContext = enterpriseContext;
    }

    public void Ensure(Permission requiredPermission)
    {
        if (!_permissionEngine.HasPermission(_enterpriseContext.CurrentRole, requiredPermission))
        {
            throw new UnauthorizedOperationException(requiredPermission);
        }
    }
}
