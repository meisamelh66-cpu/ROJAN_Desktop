using AppAccounting = Rojan.Desktop.Application.Accounting;
using AppBookings = Rojan.Desktop.Application.Bookings;
using AppCustomers = Rojan.Desktop.Application.Customers;
using AppHr = Rojan.Desktop.Application.HR;
using AppInventory = Rojan.Desktop.Application.Inventory;

namespace Rojan.Desktop.Application.Reporting;

/// <summary>
/// The cross-module aggregation core shared by <see cref="ReportExecutionQueryService"/>'s
/// three dashboard reports and <see cref="AnalyticsQueryService"/>'s
/// Analytics Dashboard summary, so both compute the exact same numbers for
/// the exact same period rather than maintaining two copies of this logic.
/// Stateless - constructed per call with the query services it needs, all
/// already-registered singletons so this has no real construction cost.
/// </summary>
internal sealed class AnalyticsAggregator
{
    private readonly AppCustomers.ICustomerQueryService _customerQueryService;
    private readonly AppBookings.IBookingQueryService _bookingQueryService;
    private readonly AppInventory.IProductQueryService _productQueryService;
    private readonly AppInventory.IInventoryQueryService _inventoryQueryService;
    private readonly AppAccounting.IInvoiceQueryService _invoiceQueryService;
    private readonly AppHr.IEmployeeQueryService _employeeQueryService;
    private readonly AppHr.IAttendanceQueryService _attendanceQueryService;
    private readonly AppHr.IPayrollQueryService _payrollQueryService;

    public AnalyticsAggregator(
        AppCustomers.ICustomerQueryService customerQueryService,
        AppBookings.IBookingQueryService bookingQueryService,
        AppInventory.IProductQueryService productQueryService,
        AppInventory.IInventoryQueryService inventoryQueryService,
        AppAccounting.IInvoiceQueryService invoiceQueryService,
        AppHr.IEmployeeQueryService employeeQueryService,
        AppHr.IAttendanceQueryService attendanceQueryService,
        AppHr.IPayrollQueryService payrollQueryService)
    {
        _customerQueryService = customerQueryService;
        _bookingQueryService = bookingQueryService;
        _productQueryService = productQueryService;
        _inventoryQueryService = inventoryQueryService;
        _invoiceQueryService = invoiceQueryService;
        _employeeQueryService = employeeQueryService;
        _attendanceQueryService = attendanceQueryService;
        _payrollQueryService = payrollQueryService;
    }

    public async Task<AnalyticsSummaryDto> AggregateAsync(DateTimeOffset start, DateTimeOffset end, string periodLabel, CancellationToken cancellationToken)
    {
        // Every fetch below is independent of every other, so they run
        // concurrently rather than sequentially - with FakeXxxRepository's
        // own per-call artificial delays (200-400ms, deliberately added so
        // Loading states are observable elsewhere in this app), awaiting
        // eight-plus calls one at a time made the Analytics Dashboard's
        // first paint take 15-20+ seconds. Task.WhenAll collapses that to
        // roughly the single slowest call.
        var invoicesTask = _invoiceQueryService.GetInvoicesAsync(cancellationToken);
        var bookingsTask = _bookingQueryService.GetBookingsAsync(cancellationToken);
        var customersTask = _customerQueryService.GetCustomersAsync(cancellationToken);
        var productsTask = _productQueryService.GetProductsAsync(cancellationToken);
        var inventoryItemsTask = _inventoryQueryService.GetInventoryItemsAsync(cancellationToken);
        var lowStockTask = _inventoryQueryService.GetLowStockItemsAsync(cancellationToken);
        var payrollSummariesTask = _payrollQueryService.GetPayrollSummariesAsync(cancellationToken);
        var employeesTask = _employeeQueryService.GetEmployeesAsync(cancellationToken);

        await Task.WhenAll(invoicesTask, bookingsTask, customersTask, productsTask, inventoryItemsTask, lowStockTask, payrollSummariesTask, employeesTask)
            .ConfigureAwait(false);

        var invoices = await invoicesTask.ConfigureAwait(false);
        var periodInvoices = invoices.Where(invoice => invoice.IssuedAt >= start && invoice.IssuedAt <= end).ToList();
        var totalRevenue = periodInvoices.Sum(invoice => invoice.Total);

        var bookings = await bookingsTask.ConfigureAwait(false);
        var periodBookings = bookings.Where(booking => booking.ScheduledAt >= start && booking.ScheduledAt <= end).ToList();

        var customers = await customersTask.ConfigureAwait(false);
        var newCustomers = customers.Count(customer => customer.LastContactedAt >= start && customer.LastContactedAt <= end);
        var retained = customers.Count(customer =>
            customer.Status is AppCustomers.CustomerStatus.Active or AppCustomers.CustomerStatus.Vip or AppCustomers.CustomerStatus.Prospect or AppCustomers.CustomerStatus.Lead);
        var retentionRate = customers.Count == 0 ? 0m : Math.Round((decimal)retained / customers.Count * 100m, 1);

        var topService = periodBookings
            .GroupBy(booking => booking.ServiceName)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault() ?? "—";
        var topSpecialist = periodBookings
            .GroupBy(booking => booking.SpecialistName)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault() ?? "—";

        var products = await productsTask.ConfigureAwait(false);
        var productsById = products.ToDictionary(product => product.Id);
        var inventoryItems = await inventoryItemsTask.ConfigureAwait(false);
        var inventoryValue = inventoryItems.Sum(item =>
            item.QuantityOnHand * (productsById.TryGetValue(item.ProductId, out var product) ? MoneyParser.Parse(product.UnitPrice) : 0m));
        var lowStockCount = (await lowStockTask.ConfigureAwait(false)).Count;

        // Payroll is inherently monthly-granular - Daily/Weekly periods
        // both report the current month's total rather than a prorated or
        // zero value, documented same as the Payroll report's own filter.
        var payrollSummaries = await payrollSummariesTask.ConfigureAwait(false);
        var payrollTotal = payrollSummaries
            .Where(summary => summary.Month == end.Month && summary.Year == end.Year)
            .Sum(summary => summary.NetSalary);

        var employees = await employeesTask.ConfigureAwait(false);
        var attendanceByEmployee = await Task.WhenAll(
            employees.Select(employee => _attendanceQueryService.GetAttendanceForEmployeeAsync(employee.Id, cancellationToken)))
            .ConfigureAwait(false);

        var totalAttendanceRecords = 0;
        var presentAttendanceRecords = 0;
        foreach (var attendance in attendanceByEmployee)
        {
            var periodRecords = attendance.Where(record =>
            {
                var recordDate = new DateTimeOffset(record.Date.ToDateTime(TimeOnly.MinValue));
                return recordDate >= start && recordDate <= end;
            }).ToList();
            totalAttendanceRecords += periodRecords.Count;
            presentAttendanceRecords += periodRecords.Count(record => record.Status == AppHr.AttendanceStatus.Present);
        }

        var attendanceRate = totalAttendanceRecords == 0 ? 0m : Math.Round((decimal)presentAttendanceRecords / totalAttendanceRecords * 100m, 1);

        return new AnalyticsSummaryDto(
            periodLabel,
            totalRevenue,
            periodBookings.Count,
            customers.Count,
            newCustomers,
            retentionRate,
            topService,
            topSpecialist,
            inventoryValue,
            lowStockCount,
            payrollTotal,
            attendanceRate,
            DateTimeOffset.Now);
    }
}
