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
        foreach (var role in Enum.GetValues<WorkspaceRole>())
        {
            Assert.Contains(Permission.DashboardView, RolePermissions.GetPermissions(role));
        }
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
