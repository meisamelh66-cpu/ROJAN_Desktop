using AppAccounting = Rojan.Desktop.Application.Accounting;
using AppBookings = Rojan.Desktop.Application.Bookings;
using AppCustomers = Rojan.Desktop.Application.Customers;
using AppHr = Rojan.Desktop.Application.HR;
using AppInventory = Rojan.Desktop.Application.Inventory;
using DomainReporting = Rojan.Desktop.Domain.Reporting;

namespace Rojan.Desktop.Application.Reporting;

/// <summary>
/// Default <see cref="IKpiEngineQueryService"/>. Revenue, Appointments,
/// Payroll, Attendance, and Growth compare a genuine current-vs-previous
/// period (real trend math via <c>Domain.Reporting.TrendCalculator</c>).
/// Customers and Inventory report their current global value with a Flat
/// trend, honestly documented below, because neither source module keeps
/// a historical snapshot this phase could compare against. Trend restates
/// Revenue's own trend as the engine's single headline figure.
/// </summary>
public sealed class KpiEngineQueryService : IKpiEngineQueryService
{
    private readonly AppCustomers.ICustomerQueryService _customerQueryService;
    private readonly AppBookings.IBookingQueryService _bookingQueryService;
    private readonly AppInventory.IProductQueryService _productQueryService;
    private readonly AppInventory.IInventoryQueryService _inventoryQueryService;
    private readonly AppAccounting.IInvoiceQueryService _invoiceQueryService;
    private readonly AppHr.IEmployeeQueryService _employeeQueryService;
    private readonly AppHr.IAttendanceQueryService _attendanceQueryService;
    private readonly AppHr.IPayrollQueryService _payrollQueryService;

    public KpiEngineQueryService(
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

    public async Task<IReadOnlyList<KpiValueDto>> GetKpisAsync(AnalyticsPeriod period, CancellationToken cancellationToken = default)
    {
        var aggregator = new AnalyticsAggregator(
            _customerQueryService, _bookingQueryService, _productQueryService, _inventoryQueryService,
            _invoiceQueryService, _employeeQueryService, _attendanceQueryService, _payrollQueryService);

        var (currentStart, currentEnd, label) = AnalyticsPeriods.Resolve(period);
        var (previousStart, previousEnd) = AnalyticsPeriods.PreviousPeriod(period);

        var currentTask = aggregator.AggregateAsync(currentStart, currentEnd, label, cancellationToken);
        var previousTask = aggregator.AggregateAsync(previousStart, previousEnd, label, cancellationToken);
        var bookingsTask = _bookingQueryService.GetBookingsAsync(cancellationToken);
        var employeesTask = _employeeQueryService.GetEmployeesAsync(cancellationToken);
        await Task.WhenAll(currentTask, previousTask, bookingsTask, employeesTask).ConfigureAwait(false);
        var current = await currentTask.ConfigureAwait(false);
        var previous = await previousTask.ConfigureAwait(false);
        var bookings = await bookingsTask.ConfigureAwait(false);
        var employees = await employeesTask.ConfigureAwait(false);

        var currentBookings = bookings.Where(booking => booking.ScheduledAt >= currentStart && booking.ScheduledAt <= currentEnd).ToList();
        var previousBookings = bookings.Where(booking => booking.ScheduledAt >= previousStart && booking.ScheduledAt <= previousEnd).ToList();

        var currentCancellationRate = ComputeRatePercent(currentBookings.Count, currentBookings.Count(booking => booking.Status == AppBookings.BookingStatus.Cancelled));
        var previousCancellationRate = ComputeRatePercent(previousBookings.Count, previousBookings.Count(booking => booking.Status == AppBookings.BookingStatus.Cancelled));

        var currentAverageTicket = current.TotalAppointments == 0 ? 0m : Math.Round(current.TotalRevenue / current.TotalAppointments, 0);
        var previousAverageTicket = previous.TotalAppointments == 0 ? 0m : Math.Round(previous.TotalRevenue / previous.TotalAppointments, 0);

        var currentCompleted = currentBookings.Where(booking => booking.Status == AppBookings.BookingStatus.Completed).ToList();
        var previousCompleted = previousBookings.Where(booking => booking.Status == AppBookings.BookingStatus.Completed).ToList();
        var currentAverageServiceTime = currentCompleted.Count == 0 ? 0m : (decimal)currentCompleted.Average(booking => booking.DurationMinutes);
        var previousAverageServiceTime = previousCompleted.Count == 0 ? 0m : (decimal)previousCompleted.Average(booking => booking.DurationMinutes);

        var activeEmployeeCount = employees.Count(employee => employee.Status == AppHr.EmployeeStatus.Active);
        var currentProductivity = activeEmployeeCount == 0 ? 0m : Math.Round(current.TotalRevenue / activeEmployeeCount, 0);
        var previousProductivity = activeEmployeeCount == 0 ? 0m : Math.Round(previous.TotalRevenue / activeEmployeeCount, 0);

        return
        [
            BuildKpi("kpi-revenue", "درآمد", KpiType.Revenue, current.TotalRevenue, previous.TotalRevenue, "تومان", label),
            BuildKpi("kpi-appointments", "نوبت‌ها", KpiType.Appointments, current.TotalAppointments, previous.TotalAppointments, string.Empty, label),
            BuildKpi("kpi-customers", "مشتریان", KpiType.Customers, current.TotalCustomers, current.TotalCustomers, string.Empty, label),
            BuildKpi("kpi-inventory", "ارزش موجودی انبار", KpiType.Inventory, current.InventoryValue, current.InventoryValue, "تومان", label),
            BuildKpi("kpi-payroll", "حقوق و دستمزد", KpiType.Payroll, current.PayrollTotal, previous.PayrollTotal, "تومان", label),
            BuildKpi("kpi-attendance", "نرخ حضور", KpiType.Attendance, current.AttendanceRatePercent, previous.AttendanceRatePercent, "%", label),
            BuildKpi("kpi-growth", "مشتریان جدید", KpiType.Growth, current.NewCustomers, previous.NewCustomers, string.Empty, label),
            BuildKpi("kpi-trend", "روند کلی", KpiType.Trend, current.TotalRevenue, previous.TotalRevenue, "تومان", label),

            // Phase 33: Enterprise Reporting & Business Intelligence Platform.
            // Profit/Expenses are an honest approximation - Payroll is the
            // only real cost data this app tracks (no dedicated Expense
            // module exists), so Expenses = Payroll and Profit = Revenue -
            // Payroll, documented the same way Customers/Inventory already
            // document their own Flat-trend limitation above.
            BuildKpi("kpi-profit", "سود", KpiType.Profit, current.TotalRevenue - current.PayrollTotal, previous.TotalRevenue - previous.PayrollTotal, "تومان", label),
            BuildKpi("kpi-expenses", "هزینه‌ها", KpiType.Expenses, current.PayrollTotal, previous.PayrollTotal, "تومان", label),
            BuildKpi("kpi-cancellation-rate", "نرخ لغو نوبت", KpiType.CancellationRate, currentCancellationRate, previousCancellationRate, "%", label),
            BuildKpi("kpi-average-ticket", "میانگین فاکتور", KpiType.AverageTicket, currentAverageTicket, previousAverageTicket, "تومان", label),
            BuildKpi("kpi-average-service-time", "میانگین زمان خدمت", KpiType.AverageServiceTime, currentAverageServiceTime, previousAverageServiceTime, "دقیقه", label),
            BuildKpi("kpi-retention", "نرخ بازگشت مشتری", KpiType.Retention, current.RetentionRatePercent, previous.RetentionRatePercent, "%", label),
            BuildKpi("kpi-employee-productivity", "بهره‌وری کارمندان", KpiType.EmployeeProductivity, currentProductivity, previousProductivity, "تومان", label),
        ];
    }

    private static decimal ComputeRatePercent(int total, int matching) =>
        total == 0 ? 0m : Math.Round((decimal)matching / total * 100m, 1);

    private static KpiValueDto BuildKpi(string id, string name, KpiType kpiType, decimal currentValue, decimal previousValue, string unit, string periodLabel)
    {
        var trend = DomainReporting.TrendCalculator.ComputeTrend(currentValue, previousValue);
        var changePercent = DomainReporting.TrendCalculator.ComputeChangePercent(currentValue, previousValue);
        return new KpiValueDto(id, name, kpiType, currentValue, previousValue, ReportingMapper.MapTrend(trend), changePercent, unit, periodLabel);
    }

    private static KpiValueDto BuildKpi(string id, string name, KpiType kpiType, int currentValue, int previousValue, string unit, string periodLabel) =>
        BuildKpi(id, name, kpiType, (decimal)currentValue, (decimal)previousValue, unit, periodLabel);
}
