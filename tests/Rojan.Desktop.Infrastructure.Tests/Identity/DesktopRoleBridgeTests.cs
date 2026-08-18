using Rojan.Desktop.Infrastructure.Identity;
using ApplicationRole = Rojan.Desktop.Application.Organizations.WorkspaceRole;
using DomainRole = Rojan.Desktop.Domain.Organizations.WorkspaceRole;

namespace Rojan.Desktop.Infrastructure.Tests.Identity;

/// <summary>
/// Phase 2A Role Bridge Cleanup: exercises <see cref="DesktopRoleBridge.ToDomainRole"/> -
/// direct coverage this conversion never had before (previously only
/// indirectly reachable via <c>IdentityContextService.GetSnapshotAsync</c>,
/// which has no live callers).
/// </summary>
public sealed class DesktopRoleBridgeTests
{
    [Theory]
    [InlineData(ApplicationRole.PlatformOwner, DomainRole.PlatformOwner)]
    [InlineData(ApplicationRole.OrganizationOwner, DomainRole.OrganizationOwner)]
    [InlineData(ApplicationRole.OrganizationManager, DomainRole.OrganizationManager)]
    [InlineData(ApplicationRole.BranchManager, DomainRole.BranchManager)]
    [InlineData(ApplicationRole.Reception, DomainRole.Reception)]
    [InlineData(ApplicationRole.Specialist, DomainRole.Specialist)]
    [InlineData(ApplicationRole.Inventory, DomainRole.Inventory)]
    [InlineData(ApplicationRole.Accounting, DomainRole.Accounting)]
    [InlineData(ApplicationRole.Hr, DomainRole.Hr)]
    [InlineData(ApplicationRole.Ai, DomainRole.Ai)]
    [InlineData(ApplicationRole.Support, DomainRole.Support)]
    [InlineData(ApplicationRole.Marketing, DomainRole.Marketing)]
    public void ToDomainRole_EveryApplicationRole_MapsToTheSameNamedDomainRole(ApplicationRole applicationRole, DomainRole expectedDomainRole)
    {
        var result = DesktopRoleBridge.ToDomainRole(applicationRole);

        Assert.Equal(expectedDomainRole, result);
    }

    [Fact]
    public void ToDomainRole_EveryEnumMember_HasAMatchingDomainCounterpart()
    {
        // Confirms the two enums haven't drifted apart - if a future member is added to one
        // without the other, this throws (Enum.Parse) rather than the theory cases above silently
        // not covering it.
        foreach (var role in Enum.GetValues<ApplicationRole>())
        {
            var result = DesktopRoleBridge.ToDomainRole(role);

            Assert.Equal(role.ToString(), result.ToString());
        }
    }
}
