namespace Rojan.Desktop.Domain.Organizations;

/// <summary>
/// The Permission Engine's core logic - a fixed, static
/// <see cref="WorkspaceRole"/> -&gt; <see cref="Permission"/> set mapping.
/// Genuine Domain logic (no I/O, no external state), same "rules live in
/// Domain" reasoning every other module's calculators/rule classes follow
/// (e.g. <c>Reporting.TrendCalculator</c>, <c>AI.BusinessHealthCalculator</c>).
/// <see cref="WorkspaceRole.PlatformOwner"/>/<see cref="WorkspaceRole.OrganizationOwner"/> are granted
/// every permission that exists (defined once, reflectively, so adding a
/// new <see cref="Permission"/> member automatically grants it to both
/// without a matching edit here); every other role has an explicit,
/// reviewable set.
/// </summary>
public static class RolePermissions
{
    private static readonly IReadOnlySet<Permission> AllPermissions =
        Enum.GetValues<Permission>().ToHashSet();

    private static readonly Dictionary<WorkspaceRole, IReadOnlySet<Permission>> Map = new()
    {
        [WorkspaceRole.PlatformOwner] = AllPermissions,
        [WorkspaceRole.OrganizationOwner] = AllPermissions,
        [WorkspaceRole.OrganizationManager] = new HashSet<Permission>
        {
            Permission.DashboardView,
            Permission.CustomerRead, Permission.CustomerEdit,
            Permission.BookingRead, Permission.BookingCreate, Permission.BookingEdit,
            Permission.CalendarView,
            Permission.ServiceRead, Permission.ServiceEdit,
            Permission.SpecialistRead, Permission.SpecialistEdit,
            Permission.InventoryRead, Permission.InventoryEdit,
            Permission.AccountingView, Permission.AccountingManage,
            Permission.HrView, Permission.HrManage,
            Permission.ReportingView, Permission.ReportingExport,
            Permission.AiUse,
            Permission.SettingsManage,
            Permission.BranchManage,
            Permission.Approve, Permission.Reject,
            Permission.ManageUsers,
        },
        [WorkspaceRole.BranchManager] = new HashSet<Permission>
        {
            Permission.DashboardView,
            Permission.CustomerRead, Permission.CustomerEdit,
            Permission.BookingRead, Permission.BookingCreate, Permission.BookingEdit,
            Permission.CalendarView,
            Permission.ServiceRead, Permission.ServiceEdit,
            Permission.SpecialistRead, Permission.SpecialistEdit,
            Permission.InventoryRead, Permission.InventoryEdit,
            Permission.AccountingView,
            Permission.HrView,
            Permission.ReportingView,
            Permission.AiUse,
            Permission.SettingsManage,
            Permission.BranchManage,
            Permission.Approve, Permission.Reject,
            Permission.ManageUsers,
        },
        [WorkspaceRole.Reception] = new HashSet<Permission>
        {
            Permission.DashboardView,
            Permission.CustomerRead, Permission.CustomerEdit,
            Permission.BookingRead, Permission.BookingCreate, Permission.BookingEdit,
            Permission.CalendarView,
            Permission.ServiceRead,
            Permission.SpecialistRead,
        },
        [WorkspaceRole.Specialist] = new HashSet<Permission>
        {
            Permission.DashboardView,
            Permission.CustomerRead,
            Permission.BookingRead, Permission.BookingEdit,
            Permission.CalendarView,
            Permission.ServiceRead,
        },
        [WorkspaceRole.Inventory] = new HashSet<Permission>
        {
            Permission.DashboardView,
            Permission.InventoryRead, Permission.InventoryEdit,
            Permission.Import,
        },
        [WorkspaceRole.Accounting] = new HashSet<Permission>
        {
            Permission.DashboardView,
            Permission.AccountingView, Permission.AccountingManage,
            Permission.ReportingView,
            Permission.Approve, Permission.Reject,
            Permission.Import,
        },
        [WorkspaceRole.Hr] = new HashSet<Permission>
        {
            Permission.DashboardView,
            Permission.HrView, Permission.HrManage,
            Permission.Approve, Permission.Reject,
        },
        [WorkspaceRole.Ai] = new HashSet<Permission>
        {
            Permission.DashboardView,
            Permission.AiUse,
            Permission.ReportingView,
        },
        [WorkspaceRole.Support] = new HashSet<Permission>
        {
            Permission.DashboardView,
            Permission.CustomerRead,
            Permission.BookingRead,
        },
        [WorkspaceRole.Marketing] = new HashSet<Permission>
        {
            Permission.DashboardView,
            Permission.CustomerRead,
            Permission.ReportingView,
            Permission.AiUse,
        },
    };

    public static IReadOnlySet<Permission> GetPermissions(WorkspaceRole role) =>
        Map.TryGetValue(role, out var permissions) ? permissions : new HashSet<Permission>();

    public static bool HasPermission(WorkspaceRole role, Permission permission) =>
        GetPermissions(role).Contains(permission);
}
