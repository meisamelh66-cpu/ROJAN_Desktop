using AppAccounting = Rojan.Desktop.Application.Accounting;
using AppBookings = Rojan.Desktop.Application.Bookings;
using AppCustomers = Rojan.Desktop.Application.Customers;
using AppHr = Rojan.Desktop.Application.HR;
using AppInventory = Rojan.Desktop.Application.Inventory;
using AppServices = Rojan.Desktop.Application.Services;
using AppSpecialists = Rojan.Desktop.Application.Specialists;

namespace Rojan.Desktop.Application.Tests.Reporting;

/// <summary>Fakes every sibling Application-layer query service <c>ReportExecutionQueryService</c>/<c>AnalyticsAggregator</c>/<c>KpiEngineQueryService</c> compose, so those services can be tested without any Infrastructure/Fake repository plumbing - just the DTO shapes they read.</summary>
internal sealed class StubCustomerQueryService(IReadOnlyList<AppCustomers.CustomerDto> customers) : AppCustomers.ICustomerQueryService
{
    public Task<IReadOnlyList<AppCustomers.CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(customers);

    public Task<IReadOnlyList<AppCustomers.CustomerDto>> SearchCustomersAsync(string searchText, CancellationToken cancellationToken = default) =>
        Task.FromResult(customers);
}

internal sealed class StubBookingQueryService(IReadOnlyList<AppBookings.BookingDto> bookings) : AppBookings.IBookingQueryService
{
    public Task<IReadOnlyList<AppBookings.BookingDto>> GetBookingsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(bookings);

    public Task<AppBookings.BookingDto?> GetBookingByIdAsync(string bookingId, CancellationToken cancellationToken = default) =>
        Task.FromResult(bookings.FirstOrDefault(b => b.Id == bookingId));
}

internal sealed class StubServiceQueryService(IReadOnlyList<AppServices.ServiceDto> services) : AppServices.IServiceQueryService
{
    public Task<IReadOnlyList<AppServices.ServiceDto>> GetServicesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(services);

    public Task<IReadOnlyList<AppServices.ServiceDto>> SearchServicesAsync(string searchText, CancellationToken cancellationToken = default) =>
        Task.FromResult(services);
}

internal sealed class StubSpecialistQueryService(IReadOnlyList<AppSpecialists.SpecialistDto> specialists) : AppSpecialists.ISpecialistQueryService
{
    public Task<IReadOnlyList<AppSpecialists.SpecialistDto>> GetSpecialistsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(specialists);

    public Task<IReadOnlyList<AppSpecialists.SpecialistDto>> SearchSpecialistsAsync(string searchText, CancellationToken cancellationToken = default) =>
        Task.FromResult(specialists);
}

internal sealed class StubProductQueryService(
    IReadOnlyList<AppInventory.ProductDto> products,
    IReadOnlyList<AppInventory.ProductCategoryDto>? categories = null,
    IReadOnlyList<AppInventory.SupplierDto>? suppliers = null) : AppInventory.IProductQueryService
{
    public Task<IReadOnlyList<AppInventory.ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(products);

    public Task<IReadOnlyList<AppInventory.ProductDto>> SearchProductsAsync(string searchText, CancellationToken cancellationToken = default) =>
        Task.FromResult(products);

    public Task<IReadOnlyList<AppInventory.ProductCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(categories ?? []);

    public Task<IReadOnlyList<AppInventory.SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(suppliers ?? []);
}

internal sealed class StubInventoryQueryService(
    IReadOnlyList<AppInventory.InventoryItemDto> items,
    IReadOnlyList<AppInventory.InventoryItemDto>? lowStockItems = null) : AppInventory.IInventoryQueryService
{
    public Task<IReadOnlyList<AppInventory.InventoryItemDto>> GetInventoryItemsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(items);

    public Task<IReadOnlyList<AppInventory.InventoryItemDto>> GetLowStockItemsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(lowStockItems ?? items.Where(i => i.IsLowStock).ToList());
}

internal sealed class StubInvoiceQueryService(IReadOnlyList<AppAccounting.InvoiceDto> invoices) : AppAccounting.IInvoiceQueryService
{
    public Task<IReadOnlyList<AppAccounting.InvoiceDto>> GetInvoicesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(invoices);

    public Task<IReadOnlyList<AppAccounting.InvoiceDto>> SearchInvoicesAsync(string searchText, CancellationToken cancellationToken = default) =>
        Task.FromResult(invoices);

    public Task<AppAccounting.InvoiceProfileDto> GetInvoiceProfileAsync(string invoiceId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by Reporting.");

    public Task<AppAccounting.CheckoutOptionsDto> GetCheckoutOptionsAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by Reporting.");
}

internal sealed class StubEmployeeQueryService(IReadOnlyList<AppHr.EmployeeDto> employees) : AppHr.IEmployeeQueryService
{
    public Task<IReadOnlyList<AppHr.EmployeeDto>> GetEmployeesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(employees);

    public Task<IReadOnlyList<AppHr.EmployeeDto>> SearchEmployeesAsync(string searchText, CancellationToken cancellationToken = default) =>
        Task.FromResult(employees);

    public Task<AppHr.EmployeeProfileDto> GetEmployeeProfileAsync(string employeeId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by Reporting.");

    public Task<AppHr.HrDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by Reporting.");
}

internal sealed class StubAttendanceQueryService(IReadOnlyDictionary<string, IReadOnlyList<AppHr.AttendanceDto>> attendanceByEmployeeId) : AppHr.IAttendanceQueryService
{
    public Task<IReadOnlyList<AppHr.AttendanceDto>> GetAttendanceForEmployeeAsync(string employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(attendanceByEmployeeId.TryGetValue(employeeId, out var records) ? records : []);

    public Task<IReadOnlyList<AppHr.AttendanceDto>> GetTodayAttendanceAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AppHr.AttendanceDto>>(attendanceByEmployeeId.Values.SelectMany(v => v).ToList());

    public Task<IReadOnlyList<AppHr.LeaveRequestDto>> GetLeaveRequestsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AppHr.LeaveRequestDto>>([]);
}

internal sealed class StubCommissionQueryService(IReadOnlyList<AppHr.CommissionTransactionDto> transactions) : AppHr.ICommissionQueryService
{
    public Task<IReadOnlyList<AppHr.CommissionRuleDto>> GetCommissionRulesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AppHr.CommissionRuleDto>>([]);

    public Task<IReadOnlyList<AppHr.CommissionTransactionDto>> GetAllCommissionTransactionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(transactions);

    public Task<IReadOnlyList<AppHr.CommissionTransactionDto>> GetCommissionHistoryForEmployeeAsync(string employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AppHr.CommissionTransactionDto>>(transactions.Where(t => t.EmployeeId == employeeId).ToList());

    public Task<decimal> GetMonthlyCommissionTotalAsync(string employeeId, int month, int year, CancellationToken cancellationToken = default) =>
        Task.FromResult(transactions.Where(t => t.EmployeeId == employeeId && t.EarnedAt.Month == month && t.EarnedAt.Year == year).Sum(t => t.CommissionAmount));
}

internal sealed class StubPayrollQueryService(IReadOnlyList<AppHr.PayrollSummaryDto> summaries) : AppHr.IPayrollQueryService
{
    public Task<IReadOnlyList<AppHr.PayrollSummaryDto>> GetPayrollSummariesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(summaries);

    public Task<AppHr.PayrollSummaryDto?> GetPayrollSummaryForEmployeeAsync(string employeeId, int month, int year, CancellationToken cancellationToken = default) =>
        Task.FromResult(summaries.FirstOrDefault(s => s.EmployeeId == employeeId && s.Month == month && s.Year == year));
}
