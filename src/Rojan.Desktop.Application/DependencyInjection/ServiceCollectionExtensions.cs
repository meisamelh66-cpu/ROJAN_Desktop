using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Application.Accounting;
using Rojan.Desktop.Application.Bookings;
using Rojan.Desktop.Application.BookingWorkflow;
using Rojan.Desktop.Application.Calendar;
using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Application.Dashboard;
using Rojan.Desktop.Application.HR;
using Rojan.Desktop.Application.Inventory;
using Rojan.Desktop.Application.Reporting;
using Rojan.Desktop.Application.Specialists;
using AppServices = Rojan.Desktop.Application.Services;

namespace Rojan.Desktop.Application.DependencyInjection;

/// <summary>
/// Composition entry point for this layer. <c>Shell</c>'s composition root
/// calls this without knowing what, if anything, it registers. The
/// Services vertical slice is aliased (<c>AppServices</c>) to avoid any
/// visual confusion with <see cref="IServiceCollection"/>/
/// <see cref="ServiceCollectionExtensions"/> in this same file - same
/// names, unrelated concepts.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDashboardQueryService, DashboardQueryService>();
        services.AddSingleton<ICustomerQueryService, CustomerQueryService>();
        services.AddSingleton<ICustomerProfileQueryService, CustomerProfileQueryService>();
        services.AddSingleton<ICustomerCommandService, CustomerCommandService>();
        services.AddSingleton<IBookingQueryService, BookingQueryService>();
        services.AddSingleton<IBookingCommandService, BookingCommandService>();
        services.AddSingleton<ISpecialistQueryService, SpecialistQueryService>();
        services.AddSingleton<ISpecialistProfileQueryService, SpecialistProfileQueryService>();
        services.AddSingleton<ISpecialistCommandService, SpecialistCommandService>();
        services.AddSingleton<AppServices.IServiceQueryService, AppServices.ServiceQueryService>();
        services.AddSingleton<AppServices.IServiceProfileQueryService, AppServices.ServiceProfileQueryService>();
        services.AddSingleton<AppServices.IServiceCommandService, AppServices.ServiceCommandService>();
        services.AddSingleton<ICalendarQueryService, CalendarQueryService>();
        services.AddSingleton<ICalendarCommandService, CalendarCommandService>();
        services.AddSingleton<IBookingWorkflowService, BookingWorkflowService>();
        services.AddSingleton<IProductQueryService, ProductQueryService>();
        services.AddSingleton<IProductProfileQueryService, ProductProfileQueryService>();
        services.AddSingleton<IInventoryQueryService, InventoryQueryService>();
        services.AddSingleton<IInventoryCommandService, InventoryCommandService>();
        services.AddSingleton<IInvoiceQueryService, InvoiceQueryService>();
        services.AddSingleton<IInvoiceCommandService, InvoiceCommandService>();
        services.AddSingleton<IPaymentQueryService, PaymentQueryService>();
        services.AddSingleton<IPaymentCommandService, PaymentCommandService>();
        services.AddSingleton<IEmployeeQueryService, EmployeeQueryService>();
        services.AddSingleton<IEmployeeCommandService, EmployeeCommandService>();
        services.AddSingleton<IAttendanceQueryService, AttendanceQueryService>();
        services.AddSingleton<IAttendanceCommandService, AttendanceCommandService>();
        services.AddSingleton<IShiftQueryService, ShiftQueryService>();
        services.AddSingleton<IShiftCommandService, ShiftCommandService>();
        services.AddSingleton<ICommissionQueryService, CommissionQueryService>();
        services.AddSingleton<ICommissionCommandService, CommissionCommandService>();
        services.AddSingleton<IPayrollQueryService, PayrollQueryService>();
        services.AddSingleton<IPayrollCommandService, PayrollCommandService>();
        services.AddSingleton<IReportCatalogQueryService, ReportCatalogQueryService>();
        services.AddSingleton<IReportExecutionQueryService, ReportExecutionQueryService>();
        services.AddSingleton<IReportSnapshotQueryService, ReportSnapshotQueryService>();
        services.AddSingleton<IReportSnapshotCommandService, ReportSnapshotCommandService>();
        services.AddSingleton<IKpiEngineQueryService, KpiEngineQueryService>();
        services.AddSingleton<IAnalyticsQueryService, AnalyticsQueryService>();
        services.AddSingleton<IReportExportService, ReportExportService>();
        return services;
    }
}
