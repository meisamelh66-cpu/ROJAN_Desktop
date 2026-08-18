namespace Rojan.Desktop.Infrastructure.Identity;

/// <summary>
/// Phase 2A Role Bridge Cleanup: the single Desktop-facing adapter for
/// converting Application's <see cref="Application.Organizations.WorkspaceRole"/>
/// into Domain's <see cref="Domain.Organizations.WorkspaceRole"/> for
/// non-authorization consumers - today, only <see cref="IdentityContextService"/>.
/// Both enums enumerate the identical set of roles by design, so a
/// name-based parse is the correct, tautology-free bridge, same reasoning
/// the private method this replaces (<c>IdentityContextService.ToDomainRole</c>)
/// already used - only its location changes, from a buried implementation
/// detail of one unrelated service to a small, dedicated, discoverable
/// adapter. Deliberately does not touch, wrap, or widen
/// <see cref="Application.Organizations.OrganizationMapper"/> - that type's
/// own <c>MapRole</c> remains <see cref="Application.Organizations.PermissionEngine"/>'s
/// separate, protected bridge, untouched by this cleanup (see
/// <c>ROJAN_Phase2A_Role_Bridge_Cleanup_Final_Plan_v1.md</c>).
/// </summary>
internal static class DesktopRoleBridge
{
    internal static Domain.Organizations.WorkspaceRole ToDomainRole(Application.Organizations.WorkspaceRole role) =>
        Enum.Parse<Domain.Organizations.WorkspaceRole>(role.ToString());
}
