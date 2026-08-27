using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Salons;

namespace Rojan.Desktop.Application.Tests.Salons;

/// <summary>
/// Remediation Phase 2 (Role Mapping Hardening): exercises
/// <see cref="SalonSessionAdapter.ToWorkspaceRole"/> - the single place
/// ROJAN_Backend's real salon-access authority (<see cref="SalonContext"/>)
/// becomes a local <see cref="WorkspaceRole"/>. No test file existed for
/// this class before this migration; every case here maps directly to
/// this task's own Phase 2/Phase 5 requirements - see
/// ROJAN_DESKTOP_RBAC_PHASE2_ROLE_MAPPING_HARDENING_REPORT_v1.md.
/// </summary>
public sealed class SalonSessionAdapterTests
{
    private readonly SalonSessionAdapter _sut = new();

    [Fact]
    public void ToWorkspaceRole_Owner_MapsToOrganizationOwner()
    {
        var context = new SalonContext("salon-1", "Rojan Salon", IsOwner: true, MembershipRole: null, Permissions: AllOwnerPermissions());

        var role = _sut.ToWorkspaceRole(context);

        Assert.Equal(WorkspaceRole.OrganizationOwner, role);
    }

    [Fact]
    public void ToWorkspaceRole_Manager_MapsToOrganizationManager()
    {
        var context = new SalonContext("salon-1", "Rojan Salon", IsOwner: false, MembershipRole: "MANAGER", Permissions: ManagerPermissions());

        var role = _sut.ToWorkspaceRole(context);

        Assert.Equal(WorkspaceRole.OrganizationManager, role);
    }

    [Fact]
    public void ToWorkspaceRole_Receptionist_MapsToReception()
    {
        var context = new SalonContext("salon-1", "Rojan Salon", IsOwner: false, MembershipRole: "RECEPTIONIST", Permissions: ReceptionistPermissions());

        var role = _sut.ToWorkspaceRole(context);

        Assert.Equal(WorkspaceRole.Reception, role);
    }

    [Fact]
    public void ToWorkspaceRole_BareSpecialistLink_MapsToSpecialist_NotReception()
    {
        // Phase 2's own central fix (RBAC Migration Map's "Gap 3"): a real backend
        // Specialist-only relationship has no SalonMembership row at all (MembershipRole is
        // null), and SalonPermissionResolver.kt grants it exactly {MANAGE_SCHEDULE_OWN} - the
        // pre-hardening mapping fell through to Reception for this exact case.
        var context = new SalonContext("salon-1", "Rojan Salon", IsOwner: false, MembershipRole: null, Permissions: new HashSet<string> { "MANAGE_SCHEDULE_OWN" });

        var role = _sut.ToWorkspaceRole(context);

        Assert.Equal(WorkspaceRole.Specialist, role);
    }

    [Fact]
    public void ToWorkspaceRole_Specialist_CannotReceiveReceptionPermissions()
    {
        var context = new SalonContext("salon-1", "Rojan Salon", IsOwner: false, MembershipRole: null, Permissions: new HashSet<string> { "MANAGE_SCHEDULE_OWN" });

        var role = _sut.ToWorkspaceRole(context);
        var localPermissions = new PermissionEngine().GetPermissions(role);

        // Reception's real local grant includes CustomerEdit/BookingCreate/BookingEdit/ServiceRead/
        // SpecialistRead - none of these belong to WorkspaceRole.Specialist's own, narrower set.
        Assert.DoesNotContain(Permission.CustomerEdit, localPermissions);
        Assert.DoesNotContain(Permission.BookingCreate, localPermissions);
        Assert.DoesNotContain(Permission.ServiceEdit, localPermissions);
        Assert.DoesNotContain(Permission.SpecialistEdit, localPermissions);
    }

    [Fact]
    public void ToWorkspaceRole_Specialist_CannotReceiveCustomerManagementPermissions()
    {
        var context = new SalonContext("salon-1", "Rojan Salon", IsOwner: false, MembershipRole: null, Permissions: new HashSet<string> { "MANAGE_SCHEDULE_OWN" });

        var role = _sut.ToWorkspaceRole(context);

        Assert.False(new PermissionEngine().HasPermission(role, Permission.CustomerEdit));
    }

    [Fact]
    public void ToWorkspaceRole_Specialist_CannotReceiveServiceManagementPermissions()
    {
        var context = new SalonContext("salon-1", "Rojan Salon", IsOwner: false, MembershipRole: null, Permissions: new HashSet<string> { "MANAGE_SCHEDULE_OWN" });

        var role = _sut.ToWorkspaceRole(context);

        Assert.False(new PermissionEngine().HasPermission(role, Permission.ServiceEdit));
    }

    [Fact]
    public void ToWorkspaceRole_Specialist_CannotReceiveStaffManagementPermissions()
    {
        var context = new SalonContext("salon-1", "Rojan Salon", IsOwner: false, MembershipRole: null, Permissions: new HashSet<string> { "MANAGE_SCHEDULE_OWN" });

        var role = _sut.ToWorkspaceRole(context);

        Assert.False(new PermissionEngine().HasPermission(role, Permission.SpecialistEdit));
    }

    [Fact]
    public void ToWorkspaceRole_UnrecognizedMembershipRoleString_MapsToUnknown_DeniedSafely()
    {
        // No real backend SalonRole value other than MANAGER/RECEPTIONIST exists today - this
        // proves the fallback is fail-closed if the backend ever introduces a new one this
        // mapping doesn't yet recognize, rather than silently defaulting to Reception.
        var context = new SalonContext("salon-1", "Rojan Salon", IsOwner: false, MembershipRole: "SOMETHING_NEW", Permissions: new HashSet<string>());

        var role = _sut.ToWorkspaceRole(context);

        Assert.Equal(WorkspaceRole.Unknown, role);
        Assert.Empty(new PermissionEngine().GetPermissions(role));
    }

    [Fact]
    public void ToWorkspaceRole_NoMembershipRoleAndNoSpecialistSignature_MapsToUnknown_DeniedSafely()
    {
        // MembershipRole is null (no membership row) but the permission set does not match the
        // real Specialist-only signature either - not a relationship this mapping recognizes.
        var context = new SalonContext("salon-1", "Rojan Salon", IsOwner: false, MembershipRole: null, Permissions: new HashSet<string>());

        var role = _sut.ToWorkspaceRole(context);

        Assert.Equal(WorkspaceRole.Unknown, role);
        Assert.False(new PermissionEngine().HasPermission(role, Permission.DashboardView));
    }

    private static HashSet<string> AllOwnerPermissions() => new()
    {
        "MANAGE_SALON", "MANAGE_MEMBERSHIP", "MANAGE_CATALOG", "MANAGE_STAFF", "MANAGE_SCHEDULE_ALL",
        "MANAGE_SCHEDULE_OWN", "VIEW_CRM", "MANAGE_CRM", "MANAGE_BOOKINGS", "MANAGE_OWN_BOOKINGS",
        "VIEW_CUSTOMER_IDENTITY", "CREATE_CUSTOMER_IDENTITY", "VIEW_CUSTOMER_BOOKING_HISTORY",
    };

    private static HashSet<string> ManagerPermissions() => new()
    {
        "MANAGE_CATALOG", "MANAGE_STAFF", "MANAGE_SCHEDULE_ALL", "VIEW_CRM", "MANAGE_CRM",
        "MANAGE_BOOKINGS", "VIEW_CUSTOMER_IDENTITY", "CREATE_CUSTOMER_IDENTITY", "VIEW_CUSTOMER_BOOKING_HISTORY",
    };

    private static HashSet<string> ReceptionistPermissions() => new()
    {
        "MANAGE_BOOKINGS", "VIEW_CUSTOMER_IDENTITY", "CREATE_CUSTOMER_IDENTITY", "VIEW_CUSTOMER_BOOKING_HISTORY",
    };
}
