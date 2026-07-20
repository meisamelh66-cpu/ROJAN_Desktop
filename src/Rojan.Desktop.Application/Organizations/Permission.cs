namespace Rojan.Desktop.Application.Organizations;

/// <summary>Application's own copy of the permission concept - every Domain enum gets a distinct Application-layer copy, mapped via an internal Mapper, so Presentation never binds to a Domain-shaped type (same convention since Phase 19A's <c>Dashboard.TrendDirection</c>).</summary>
#pragma warning disable CA1711 // "Permission" is the exact, spec-accurate domain term - see Domain.Organizations.Permission's own suppression for the full rationale.
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
