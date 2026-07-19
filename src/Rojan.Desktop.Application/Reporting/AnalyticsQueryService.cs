using System.Globalization;
using AppAccounting = Rojan.Desktop.Application.Accounting;
using AppBookings = Rojan.Desktop.Application.Bookings;
using AppCustomers = Rojan.Desktop.Application.Customers;
using AppHr = Rojan.Desktop.Application.HR;
using AppInventory = Rojan.Desktop.Application.Inventory;

namespace Rojan.Desktop.Application.Reporting;

/// <summary>
/// Default <see cref="IAnalyticsQueryService"/> - the summary half reuses
/// <see cref="AnalyticsAggregator"/> (same numbers the KPI Engine and
/// Dashboard reports show); the chart half builds three real,
/// data-derived <see cref="ChartDefinitionDto"/>s with no external
/// charting library involved (Presentation renders them as native
/// proportional bars - see <c>Rojan.Desktop.Presentation.Controls.Analytics</c>).
/// </summary>
public sealed class AnalyticsQueryService : IAnalyticsQueryService
{
    private readonly AppCustomers.ICustomerQueryService _customerQueryService;
    private readonly AppBookings.IBookingQueryService _bookingQueryService;
    private readonly AppInventory.IProductQueryService _productQueryService;
    private readonly AppInventory.IInventoryQueryService _inventoryQueryService;
    private readonly AppAccounting.IInvoiceQueryService _invoiceQueryService;
    private readonly AppHr.IEmployeeQueryService _employeeQueryService;
    private readonly AppHr.IAttendanceQueryService _attendanceQueryService;
    private readonly AppHr.IPayrollQueryService _payrollQueryService;

    public AnalyticsQueryService(
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

    public async Task<AnalyticsSummaryDto> GetAnalyticsSummaryAsync(AnalyticsPeriod period, CancellationToken cancellationToken = default)
    {
        var aggregator = BuildAggregator();
        var (start, end, label) = AnalyticsPeriods.Resolve(period);
        return await aggregator.AggregateAsync(start, end, label, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ChartDefinitionDto>> GetDashboardChartsAsync(AnalyticsPeriod period, CancellationToken cancellationToken = default)
    {
        var invoices = await _invoiceQueryService.GetInvoicesAsync(cancellationToken).ConfigureAwait(false);
        var bookings = await _bookingQueryService.GetBookingsAsync(cancellationToken).ConfigureAwait(false);

        return
        [
            BuildRevenueByDayChart(invoices),
            BuildAppointmentsByStatusChart(bookings),
            BuildTopServicesChart(bookings),
        ];
    }

    private AnalyticsAggregator BuildAggregator() => new(
        _customerQueryService, _bookingQueryService, _productQueryService, _inventoryQueryService,
        _invoiceQueryService, _employeeQueryService, _attendanceQueryService, _payrollQueryService);

    private static ChartDefinitionDto BuildRevenueByDayChart(IReadOnlyList<AppAccounting.InvoiceDto> invoices)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.Now.Date);
        var days = Enumerable.Range(0, 7).Select(offset => today.AddDays(offset - 6)).ToList();
        var byDay = invoices.ToLookup(invoice => DateOnly.FromDateTime(invoice.IssuedAt.Date));

        var categories = days.Select(day => day.ToString("MMM d", CultureInfo.InvariantCulture)).ToList();
        var values = days.Select(day => byDay[day].Sum(invoice => invoice.Total)).ToList();

        return new ChartDefinitionDto("chart-revenue-by-day", "Revenue - Last 7 Days", ChartType.Column, categories, [new ChartSeriesDto("Revenue", values)]);
    }

    private static ChartDefinitionDto BuildAppointmentsByStatusChart(IReadOnlyList<AppBookings.BookingDto> bookings)
    {
        var byStatus = bookings.GroupBy(booking => booking.Status).OrderByDescending(group => group.Count()).ToList();
        var categories = byStatus.Select(group => group.Key.ToString()).ToList();
        var values = byStatus.Select(group => (decimal)group.Count()).ToList();

        return new ChartDefinitionDto("chart-appointments-by-status", "Appointments by Status", ChartType.Pie, categories, [new ChartSeriesDto("Appointments", values)]);
    }

    private static ChartDefinitionDto BuildTopServicesChart(IReadOnlyList<AppBookings.BookingDto> bookings)
    {
        var topServices = bookings
            .GroupBy(booking => booking.ServiceName)
            .OrderByDescending(group => group.Count())
            .Take(5)
            .ToList();

        var categories = topServices.Select(group => group.Key).ToList();
        var values = topServices.Select(group => (decimal)group.Count()).ToList();

        return new ChartDefinitionDto("chart-top-services", "Top Services", ChartType.Bar, categories, [new ChartSeriesDto("Bookings", values)]);
    }
}
