using Rojan.Desktop.Domain.Organizations;

namespace Rojan.Desktop.Domain.Tests.Organizations;

/// <summary>Exercises the Permission Engine's core logic - the static <see cref="RolePermissions"/> WorkspaceRole -&gt; Permission mapping.</summary>
public sealed class RolePermissionsTests
{
    [Theory]
    [InlineData(WorkspaceRole.PlatformOwner)]
    [InlineData(WorkspaceRole.OrganizationOwner)]
    public void GetPermissions_ForOwnerRoles_GrantsEveryPermission(WorkspaceRole role)
    {
        var permissions = RolePermissions.GetPermissions(role);

        Assert.Equal(Enum.GetValues<Permission>().Length, permissions.Count);
        Assert.All(Enum.GetValues<Permission>(), permission => Assert.Contains(permission, permissions));
    }

    [Fact]
    public void HasPermission_ReceptionReadingCustomer_ReturnsTrue()
    {
        Assert.True(RolePermissions.HasPermission(WorkspaceRole.Reception, Permission.CustomerRead));
    }

    [Fact]
    public void HasPermission_ReceptionManagingInventory_ReturnsFalse()
    {
        Assert.False(RolePermissions.HasPermission(WorkspaceRole.Reception, Permission.InventoryEdit));
    }

    [Fact]
    public void HasPermission_SpecialistEditingCustomer_ReturnsFalse()
    {
        Assert.False(RolePermissions.HasPermission(WorkspaceRole.Specialist, Permission.CustomerEdit));
    }

    [Fact]
    public void HasPermission_InventoryRoleUsingAccounting_ReturnsFalse()
    {
        Assert.False(RolePermissions.HasPermission(WorkspaceRole.Inventory, Permission.AccountingView));
    }

    [Fact]
    public void HasPermission_BranchManagerManagingBranch_ReturnsTrue()
    {
        Assert.True(RolePermissions.HasPermission(WorkspaceRole.BranchManager, Permission.BranchManage));
    }

    [Fact]
    public void HasPermission_BranchManagerManagingOrganization_ReturnsFalse()
    {
        Assert.False(RolePermissions.HasPermission(WorkspaceRole.BranchManager, Permission.OrganizationManage));
    }

    [Fact]
    public void GetPermissions_EveryNonOwnerRole_AlwaysIncludesDashboardView()
    {
        // Remediation Phase 2 (Role Mapping Hardening): WorkspaceRole.Unknown is the deliberate
        // exception to this invariant - it exists specifically to deny every permission, including
        // DashboardView, for a session whose real backend role could not be recognized. See its
        // own doc comment and GetPermissions_UnknownRole_GrantsNoPermissions below.
        foreach (var role in Enum.GetValues<WorkspaceRole>().Where(role => role != WorkspaceRole.Unknown))
        {
            Assert.Contains(Permission.DashboardView, RolePermissions.GetPermissions(role));
        }
    }

    [Fact]
    public void GetPermissions_UnknownRole_GrantsNoPermissions()
    {
        var permissions = RolePermissions.GetPermissions(WorkspaceRole.Unknown);

        Assert.Empty(permissions);
    }

    [Fact]
    public void HasPermission_UnknownRoleRequestingDashboardView_ReturnsFalse()
    {
        // Not even the one permission every other role always has (see the test above) - a
        // deliberately fail-closed default, never a fail-open one.
        Assert.False(RolePermissions.HasPermission(WorkspaceRole.Unknown, Permission.DashboardView));
    }

    [Fact]
    public void HasPermission_AccountingApprovingInvoice_ReturnsTrue()
    {
        Assert.True(RolePermissions.HasPermission(WorkspaceRole.Accounting, Permission.Approve));
    }

    [Fact]
    public void HasPermission_HrApprovingLeave_ReturnsTrue()
    {
        Assert.True(RolePermissions.HasPermission(WorkspaceRole.Hr, Permission.Approve));
    }

    [Fact]
    public void HasPermission_InventoryImportingStock_ReturnsTrue()
    {
        Assert.True(RolePermissions.HasPermission(WorkspaceRole.Inventory, Permission.Import));
    }

    [Fact]
    public void HasPermission_ReceptionApproving_ReturnsFalse()
    {
        Assert.False(RolePermissions.HasPermission(WorkspaceRole.Reception, Permission.Approve));
    }

    [Fact]
    public void HasPermission_BranchManagerManagingUsers_ReturnsTrue()
    {
        Assert.True(RolePermissions.HasPermission(WorkspaceRole.BranchManager, Permission.ManageUsers));
    }

    [Fact]
    public void HasPermission_MarketingReadingCustomersAndUsingAi_ReturnsTrue()
    {
        Assert.True(RolePermissions.HasPermission(WorkspaceRole.Marketing, Permission.CustomerRead));
        Assert.True(RolePermissions.HasPermission(WorkspaceRole.Marketing, Permission.AiUse));
    }

    [Fact]
    public void HasPermission_MarketingEditingCustomersOrManagingAccounting_ReturnsFalse()
    {
        Assert.False(RolePermissions.HasPermission(WorkspaceRole.Marketing, Permission.CustomerEdit));
        Assert.False(RolePermissions.HasPermission(WorkspaceRole.Marketing, Permission.AccountingManage));
    }
}
