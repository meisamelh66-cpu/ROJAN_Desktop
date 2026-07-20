namespace Rojan.Desktop.Application.Organizations;

/// <summary>Application-layer façade over <c>Domain.Organizations.RolePermissions</c> - the Permission Engine every consumer (navigation filtering, command/button enablement) actually calls, never the Domain static class directly, so Presentation never binds to a Domain-shaped type.</summary>
public interface IPermissionEngine
{
    public bool HasPermission(WorkspaceRole role, Permission permission);

    public IReadOnlySet<Permission> GetPermissions(WorkspaceRole role);
}
