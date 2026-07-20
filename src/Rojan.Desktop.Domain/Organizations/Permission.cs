namespace Rojan.Desktop.Domain.Organizations;

/// <summary>
/// One fine-grained capability the Permission Engine (<see cref="RolePermissions"/>)
/// grants per <see cref="WorkspaceRole"/> - drives navigation visibility
/// and, at each consuming page's own discretion, command/button
/// enablement. One member per module capability rather than a single
/// per-module flag, so (for example) Reception can read a customer
/// without being able to edit one.
/// </summary>
#pragma warning disable CA1711 // "Permission" is the exact, spec-accurate domain term (Permission Engine, Customer.Read/Edit-style permissions) - a rename to satisfy this rule's generic "don't end in Permission" heuristic would hurt clarity more than it helps.
public enum Permission
#pragma warning restore CA1711
{
    DashboardView,
    CustomerRead,
    CustomerEdit,
    BookingRead,
    BookingCreate,
    BookingEdit,
    CalendarView,
    ServiceRead,
    ServiceEdit,
    SpecialistRead,
    SpecialistEdit,
    InventoryRead,
    InventoryEdit,
    AccountingView,
    AccountingManage,
    HrView,
    HrManage,
    ReportingView,
    ReportingExport,
    AiUse,
    SettingsManage,
    OrganizationManage,
    BranchManage,
    Approve,
    Reject,
    Import,
    ManageUsers,
}
