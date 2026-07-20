using DomainOrg = Rojan.Desktop.Domain.Organizations;

namespace Rojan.Desktop.Application.Organizations;

/// <summary>Domain&lt;-&gt;Application mapping for the Enterprise Multi-Branch platform - internal, only this project's own services call it, matching every other module's Mapper convention.</summary>
internal static class OrganizationMapper
{
    public static OrganizationDto MapOrganization(DomainOrg.Organization organization) =>
        new(organization.Id, organization.Name, organization.LegalName, organization.Logo, organization.BrandColor, organization.TaxInformation, MapPlan(organization.Subscription), MapStatus(organization.Status), organization.CreatedDate);

    public static DomainOrg.Organization MapOrganization(OrganizationDto organization) =>
        new(organization.Id, organization.Name, organization.LegalName, organization.Logo, organization.BrandColor, organization.TaxInformation, MapPlan(organization.Subscription), MapStatus(organization.Status), organization.CreatedDate);

    public static BranchDto MapBranch(DomainOrg.Branch branch) =>
        new(branch.Id, branch.OrganizationId, branch.Name, branch.Code, branch.Address, branch.Phone, branch.Email, branch.Manager, branch.TimeZone, branch.Currency, MapBranchStatus(branch.Status));

    public static DomainOrg.Branch MapBranch(BranchDto branch) =>
        new(branch.Id, branch.OrganizationId, branch.Name, branch.Code, branch.Address, branch.Phone, branch.Email, branch.Manager, branch.TimeZone, branch.Currency, MapBranchStatus(branch.Status));

    public static BranchSettingsDto MapSettings(DomainOrg.BranchSettings settings) => new(
        settings.BranchId,
        new BusinessHoursDto(settings.BusinessHours.OpenTime, settings.BusinessHours.CloseTime),
        settings.WorkingDays,
        settings.VatPercentage,
        new ReceiptSettingsDto(settings.ReceiptSettings.HeaderText, settings.ReceiptSettings.FooterText, settings.ReceiptSettings.ShowLogo),
        new AppointmentRulesDto(settings.AppointmentRules.MinNoticeHours, settings.AppointmentRules.MaxAdvanceBookingDays, settings.AppointmentRules.AllowSameDayBooking),
        new NotificationSettingsDto(settings.NotificationSettings.EmailEnabled, settings.NotificationSettings.SmsEnabled, settings.NotificationSettings.ReminderHoursBeforeAppointment));

    public static DomainOrg.BranchSettings MapSettings(BranchSettingsDto settings) => new(
        settings.BranchId,
        new DomainOrg.BusinessHours(settings.BusinessHours.OpenTime, settings.BusinessHours.CloseTime),
        settings.WorkingDays,
        settings.VatPercentage,
        new DomainOrg.ReceiptSettings(settings.ReceiptSettings.HeaderText, settings.ReceiptSettings.FooterText, settings.ReceiptSettings.ShowLogo),
        new DomainOrg.AppointmentRules(settings.AppointmentRules.MinNoticeHours, settings.AppointmentRules.MaxAdvanceBookingDays, settings.AppointmentRules.AllowSameDayBooking),
        new DomainOrg.NotificationSettings(settings.NotificationSettings.EmailEnabled, settings.NotificationSettings.SmsEnabled, settings.NotificationSettings.ReminderHoursBeforeAppointment));

    public static SubscriptionPlan MapPlan(DomainOrg.SubscriptionPlan plan) => plan switch
    {
        DomainOrg.SubscriptionPlan.Trial => SubscriptionPlan.Trial,
        DomainOrg.SubscriptionPlan.Starter => SubscriptionPlan.Starter,
        DomainOrg.SubscriptionPlan.Professional => SubscriptionPlan.Professional,
        DomainOrg.SubscriptionPlan.Enterprise => SubscriptionPlan.Enterprise,
        _ => throw new ArgumentOutOfRangeException(nameof(plan), plan, "Unknown subscription plan."),
    };

    public static DomainOrg.SubscriptionPlan MapPlan(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Trial => DomainOrg.SubscriptionPlan.Trial,
        SubscriptionPlan.Starter => DomainOrg.SubscriptionPlan.Starter,
        SubscriptionPlan.Professional => DomainOrg.SubscriptionPlan.Professional,
        SubscriptionPlan.Enterprise => DomainOrg.SubscriptionPlan.Enterprise,
        _ => throw new ArgumentOutOfRangeException(nameof(plan), plan, "Unknown subscription plan."),
    };

    public static OrganizationStatus MapStatus(DomainOrg.OrganizationStatus status) => status switch
    {
        DomainOrg.OrganizationStatus.Trial => OrganizationStatus.Trial,
        DomainOrg.OrganizationStatus.Active => OrganizationStatus.Active,
        DomainOrg.OrganizationStatus.Suspended => OrganizationStatus.Suspended,
        DomainOrg.OrganizationStatus.Cancelled => OrganizationStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown organization status."),
    };

    public static DomainOrg.OrganizationStatus MapStatus(OrganizationStatus status) => status switch
    {
        OrganizationStatus.Trial => DomainOrg.OrganizationStatus.Trial,
        OrganizationStatus.Active => DomainOrg.OrganizationStatus.Active,
        OrganizationStatus.Suspended => DomainOrg.OrganizationStatus.Suspended,
        OrganizationStatus.Cancelled => DomainOrg.OrganizationStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown organization status."),
    };

    public static BranchStatus MapBranchStatus(DomainOrg.BranchStatus status) => status switch
    {
        DomainOrg.BranchStatus.Active => BranchStatus.Active,
        DomainOrg.BranchStatus.Inactive => BranchStatus.Inactive,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown branch status."),
    };

    public static DomainOrg.BranchStatus MapBranchStatus(BranchStatus status) => status switch
    {
        BranchStatus.Active => DomainOrg.BranchStatus.Active,
        BranchStatus.Inactive => DomainOrg.BranchStatus.Inactive,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown branch status."),
    };

    public static Permission MapPermission(DomainOrg.Permission permission) => permission switch
    {
        DomainOrg.Permission.DashboardView => Permission.DashboardView,
        DomainOrg.Permission.CustomerRead => Permission.CustomerRead,
        DomainOrg.Permission.CustomerEdit => Permission.CustomerEdit,
        DomainOrg.Permission.BookingRead => Permission.BookingRead,
        DomainOrg.Permission.BookingCreate => Permission.BookingCreate,
        DomainOrg.Permission.BookingEdit => Permission.BookingEdit,
        DomainOrg.Permission.CalendarView => Permission.CalendarView,
        DomainOrg.Permission.ServiceRead => Permission.ServiceRead,
        DomainOrg.Permission.ServiceEdit => Permission.ServiceEdit,
        DomainOrg.Permission.SpecialistRead => Permission.SpecialistRead,
        DomainOrg.Permission.SpecialistEdit => Permission.SpecialistEdit,
        DomainOrg.Permission.InventoryRead => Permission.InventoryRead,
        DomainOrg.Permission.InventoryEdit => Permission.InventoryEdit,
        DomainOrg.Permission.AccountingView => Permission.AccountingView,
        DomainOrg.Permission.AccountingManage => Permission.AccountingManage,
        DomainOrg.Permission.HrView => Permission.HrView,
        DomainOrg.Permission.HrManage => Permission.HrManage,
        DomainOrg.Permission.ReportingView => Permission.ReportingView,
        DomainOrg.Permission.ReportingExport => Permission.ReportingExport,
        DomainOrg.Permission.AiUse => Permission.AiUse,
        DomainOrg.Permission.SettingsManage => Permission.SettingsManage,
        DomainOrg.Permission.OrganizationManage => Permission.OrganizationManage,
        DomainOrg.Permission.BranchManage => Permission.BranchManage,
        _ => throw new ArgumentOutOfRangeException(nameof(permission), permission, "Unknown permission."),
    };

    public static DomainOrg.Permission MapPermission(Permission permission) => permission switch
    {
        Permission.DashboardView => DomainOrg.Permission.DashboardView,
        Permission.CustomerRead => DomainOrg.Permission.CustomerRead,
        Permission.CustomerEdit => DomainOrg.Permission.CustomerEdit,
        Permission.BookingRead => DomainOrg.Permission.BookingRead,
        Permission.BookingCreate => DomainOrg.Permission.BookingCreate,
        Permission.BookingEdit => DomainOrg.Permission.BookingEdit,
        Permission.CalendarView => DomainOrg.Permission.CalendarView,
        Permission.ServiceRead => DomainOrg.Permission.ServiceRead,
        Permission.ServiceEdit => DomainOrg.Permission.ServiceEdit,
        Permission.SpecialistRead => DomainOrg.Permission.SpecialistRead,
        Permission.SpecialistEdit => DomainOrg.Permission.SpecialistEdit,
        Permission.InventoryRead => DomainOrg.Permission.InventoryRead,
        Permission.InventoryEdit => DomainOrg.Permission.InventoryEdit,
        Permission.AccountingView => DomainOrg.Permission.AccountingView,
        Permission.AccountingManage => DomainOrg.Permission.AccountingManage,
        Permission.HrView => DomainOrg.Permission.HrView,
        Permission.HrManage => DomainOrg.Permission.HrManage,
        Permission.ReportingView => DomainOrg.Permission.ReportingView,
        Permission.ReportingExport => DomainOrg.Permission.ReportingExport,
        Permission.AiUse => DomainOrg.Permission.AiUse,
        Permission.SettingsManage => DomainOrg.Permission.SettingsManage,
        Permission.OrganizationManage => DomainOrg.Permission.OrganizationManage,
        Permission.BranchManage => DomainOrg.Permission.BranchManage,
        _ => throw new ArgumentOutOfRangeException(nameof(permission), permission, "Unknown permission."),
    };

    public static WorkspaceRole MapRole(DomainOrg.WorkspaceRole role) => role switch
    {
        DomainOrg.WorkspaceRole.PlatformOwner => WorkspaceRole.PlatformOwner,
        DomainOrg.WorkspaceRole.OrganizationOwner => WorkspaceRole.OrganizationOwner,
        DomainOrg.WorkspaceRole.OrganizationManager => WorkspaceRole.OrganizationManager,
        DomainOrg.WorkspaceRole.BranchManager => WorkspaceRole.BranchManager,
        DomainOrg.WorkspaceRole.Reception => WorkspaceRole.Reception,
        DomainOrg.WorkspaceRole.Specialist => WorkspaceRole.Specialist,
        DomainOrg.WorkspaceRole.Inventory => WorkspaceRole.Inventory,
        DomainOrg.WorkspaceRole.Accounting => WorkspaceRole.Accounting,
        DomainOrg.WorkspaceRole.Hr => WorkspaceRole.Hr,
        DomainOrg.WorkspaceRole.Ai => WorkspaceRole.Ai,
        DomainOrg.WorkspaceRole.Support => WorkspaceRole.Support,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown workspace role."),
    };

    public static DomainOrg.WorkspaceRole MapRole(WorkspaceRole role) => role switch
    {
        WorkspaceRole.PlatformOwner => DomainOrg.WorkspaceRole.PlatformOwner,
        WorkspaceRole.OrganizationOwner => DomainOrg.WorkspaceRole.OrganizationOwner,
        WorkspaceRole.OrganizationManager => DomainOrg.WorkspaceRole.OrganizationManager,
        WorkspaceRole.BranchManager => DomainOrg.WorkspaceRole.BranchManager,
        WorkspaceRole.Reception => DomainOrg.WorkspaceRole.Reception,
        WorkspaceRole.Specialist => DomainOrg.WorkspaceRole.Specialist,
        WorkspaceRole.Inventory => DomainOrg.WorkspaceRole.Inventory,
        WorkspaceRole.Accounting => DomainOrg.WorkspaceRole.Accounting,
        WorkspaceRole.Hr => DomainOrg.WorkspaceRole.Hr,
        WorkspaceRole.Ai => DomainOrg.WorkspaceRole.Ai,
        WorkspaceRole.Support => DomainOrg.WorkspaceRole.Support,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown workspace role."),
    };
}
