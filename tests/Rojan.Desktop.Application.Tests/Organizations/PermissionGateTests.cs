using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Tests.Organizations;

/// <summary>
/// Exercises <see cref="PermissionGate"/> - the single enforcement point
/// every module's mutating command service now calls through (see each
/// module's own <c>*PermissionGate</c> decorator, e.g.
/// <c>Customers.CustomerCommandServicePermissionGate</c>). "Unauthorized
/// operations must never execute" is proven here at its source: a role
/// that lacks the permission gets an exception, not a silent no-op.
/// </summary>
public sealed class PermissionGateTests
{
    [Fact]
    public void Ensure_RoleHasPermission_DoesNotThrow()
    {
        var gate = new PermissionGate(new PermissionEngine(), new StubEnterpriseContext { CurrentRole = WorkspaceRole.PlatformOwner });

        var exception = Record.Exception(() => gate.Ensure(Permission.OrganizationManage));

        Assert.Null(exception);
    }

    [Fact]
    public void Ensure_RoleLacksPermission_ThrowsUnauthorizedOperationException()
    {
        var gate = new PermissionGate(new PermissionEngine(), new StubEnterpriseContext { CurrentRole = WorkspaceRole.Reception });

        var exception = Assert.Throws<UnauthorizedOperationException>(() => gate.Ensure(Permission.AccountingManage));

        Assert.Equal(Permission.AccountingManage, exception.RequiredPermission);
    }

    [Fact]
    public void Ensure_ReceptionCreatingBooking_DoesNotThrow()
    {
        var gate = new PermissionGate(new PermissionEngine(), new StubEnterpriseContext { CurrentRole = WorkspaceRole.Reception });

        var exception = Record.Exception(() => gate.Ensure(Permission.BookingCreate));

        Assert.Null(exception);
    }

    [Fact]
    public void Ensure_InventoryRoleManagingOrganization_Throws()
    {
        var gate = new PermissionGate(new PermissionEngine(), new StubEnterpriseContext { CurrentRole = WorkspaceRole.Inventory });

        Assert.Throws<UnauthorizedOperationException>(() => gate.Ensure(Permission.OrganizationManage));
    }
}
