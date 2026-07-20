using DomainOrg = Rojan.Desktop.Domain.Organizations;

namespace Rojan.Desktop.Application.Organizations;

public sealed class PermissionEngine : IPermissionEngine
{
    public bool HasPermission(WorkspaceRole role, Permission permission) =>
        DomainOrg.RolePermissions.HasPermission(OrganizationMapper.MapRole(role), OrganizationMapper.MapPermission(permission));

    public IReadOnlySet<Permission> GetPermissions(WorkspaceRole role) =>
        DomainOrg.RolePermissions.GetPermissions(OrganizationMapper.MapRole(role))
            .Select(OrganizationMapper.MapPermission)
            .ToHashSet();
}
