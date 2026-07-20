using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Tests.Organizations;

/// <summary>Exercises <see cref="PermissionEngine"/>, the Application-layer wrapper over <c>Domain.Organizations.RolePermissions</c> that Presentation consumes.</summary>
public sealed class PermissionEngineTests
{
    private readonly PermissionEngine _engine = new();

    [Fact]
    public void HasPermission_PlatformOwner_HasEveryPermission()
    {
        foreach (var permission in Enum.GetValues<Permission>())
        {
            Assert.True(_engine.HasPermission(WorkspaceRole.PlatformOwner, permission));
        }
    }

    [Fact]
    public void HasPermission_ReceptionCreatingBooking_ReturnsTrue()
    {
        Assert.True(_engine.HasPermission(WorkspaceRole.Reception, Permission.BookingCreate));
    }

    [Fact]
    public void HasPermission_ReceptionManagingOrganization_ReturnsFalse()
    {
        Assert.False(_engine.HasPermission(WorkspaceRole.Reception, Permission.OrganizationManage));
    }

    [Fact]
    public void GetPermissions_HrRole_IncludesHrManageButNotAccounting()
    {
        var permissions = _engine.GetPermissions(WorkspaceRole.Hr);

        Assert.Contains(Permission.HrManage, permissions);
        Assert.DoesNotContain(Permission.AccountingManage, permissions);
    }
}
