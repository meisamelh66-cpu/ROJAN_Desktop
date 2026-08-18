using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Tests.Organizations;

/// <summary>
/// Exercises <see cref="BackendPermissionGate"/> - the sibling of
/// <see cref="PermissionGateTests"/>'s own <see cref="PermissionGate"/>,
/// for backend-sourced permission strings instead of <c>RolePermissions</c>-
/// sourced ones. No real decorator calls this yet (Phase 3A is plumbing
/// only, see ROJAN_Phase3A_Permission_Consumer_Adapter_Implementation_Plan_v1.md
/// checkpoint 3) - these tests exercise the gate entirely in isolation,
/// against a stub <see cref="IEnterpriseContext"/> with a known
/// <see cref="IEnterpriseContext.BackendPermissions"/> set.
/// </summary>
public sealed class BackendPermissionGateTests
{
    [Fact]
    public void EnsureBackend_PermissionPresent_DoesNotThrow()
    {
        var gate = new BackendPermissionGate(new StubEnterpriseContext { BackendPermissions = new HashSet<string> { "MANAGE_CRM" } });

        var exception = Record.Exception(() => gate.EnsureBackend("MANAGE_CRM"));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureBackend_PermissionAbsent_ThrowsUnauthorizedOperationException()
    {
        var gate = new BackendPermissionGate(new StubEnterpriseContext { BackendPermissions = new HashSet<string> { "MANAGE_CRM" } });

        var exception = Assert.Throws<UnauthorizedOperationException>(() => gate.EnsureBackend("MANAGE_STAFF"));

        Assert.Contains("MANAGE_STAFF", exception.Message);
    }

    [Fact]
    public void EnsureBackend_EmptyPermissionSet_AlwaysThrows()
    {
        // The exact shape a NoBusinessContext/DemoContext session's BackendPermissions has -
        // confirms the gate denies by default rather than by an explicit "not present" comparison
        // that could be tricked by an empty-set edge case.
        var gate = new BackendPermissionGate(new StubEnterpriseContext { BackendPermissions = new HashSet<string>() });

        Assert.Throws<UnauthorizedOperationException>(() => gate.EnsureBackend("MANAGE_CRM"));
    }

    [Fact]
    public void EnsureBackend_MultiplePermissionsPresent_OnlyChecksTheOneRequested()
    {
        var gate = new BackendPermissionGate(new StubEnterpriseContext { BackendPermissions = new HashSet<string> { "MANAGE_CATALOG", "MANAGE_STAFF", "MANAGE_BOOKINGS" } });

        Assert.Null(Record.Exception(() => gate.EnsureBackend("MANAGE_STAFF")));
        Assert.Throws<UnauthorizedOperationException>(() => gate.EnsureBackend("MANAGE_SALON"));
    }
}
