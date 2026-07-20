using Rojan.Desktop.Application.Reporting;
using AppAccounting = Rojan.Desktop.Application.Accounting;
using AppBookings = Rojan.Desktop.Application.Bookings;
using AppCustomers = Rojan.Desktop.Application.Customers;
using AppHr = Rojan.Desktop.Application.HR;
using AppInventory = Rojan.Desktop.Application.Inventory;

namespace Rojan.Desktop.Application.Tests.Reporting;

public sealed class KpiEngineQueryServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Now;

    private static KpiEngineQueryService CreateSut(
        IReadOnlyList<AppAccounting.InvoiceDto> invoices,
        IReadOnlyList<AppBookings.BookingDto>? bookings = null,
        IReadOnlyList<AppCustomers.CustomerDto>? customers = null,
        IReadOnlyList<AppHr.PayrollSummaryDto>? payrollSummaries = null) => new(
        new StubCustomerQueryService(customers ?? []),
        new StubBookingQueryService(bookings ?? []),
        new StubProductQueryService([]),
        new StubInventoryQueryService([]),
        new StubInvoiceQueryService(invoices),
        new StubEmployeeQueryService([]),
        new StubAttendanceQueryService(new Dictionary<string, IReadOnlyList<AppHr.AttendanceDto>>()),
        new StubPayrollQueryService(payrollSummaries ?? []));

    [Fact]
    public async Task GetKpisAsync_ReturnsExactlyEightKpis()
    {
        var sut = CreateSut([]);

        var kpis = await sut.GetKpisAsync(AnalyticsPeriod.Daily);

        Assert.Equal(8, kpis.Count);
        Assert.Contains(kpis, k => k.KpiType == KpiType.Revenue);
        Assert.Contains(kpis, k => k.KpiType == KpiType.Appointments);
        Assert.Contains(kpis, k => k.KpiType == KpiType.Customers);
        Assert.Contains(kpis, k => k.KpiType == KpiType.Inventory);
        Assert.Contains(kpis, k => k.KpiType == KpiType.Payroll);
        Assert.Contains(kpis, k => k.KpiType == KpiType.Attendance);
        Assert.Contains(kpis, k => k.KpiType == KpiType.Growth);
        Assert.Contains(kpis, k => k.KpiType == KpiType.Trend);
    }

    [Fact]
    public async Task GetKpisAsync_RevenueKpi_ComputesGenuineUpTrendFromTodayVsYesterday()
    {
        IReadOnlyList<AppAccounting.InvoiceDto> invoices =
        [
            new("invoice-today", "c1", "Alice", "b1", "BK-1", Now, AppAccounting.InvoiceStatus.Paid, 100m, 0m, 200m, string.Empty),
            new("invoice-yesterday", "c1", "Alice", "b2", "BK-2", Now.AddDays(-1), AppAccounting.InvoiceStatus.Paid, 50m, 0m, 100m, string.Empty),
        ];
        var sut = CreateSut(invoices);

        var kpis = await sut.GetKpisAsync(AnalyticsPeriod.Daily);

        var revenue = kpis.Single(k => k.KpiType == KpiType.Revenue);
        Assert.Equal(200m, revenue.Value);
        Assert.Equal(100m, revenue.PreviousValue);
        Assert.Equal(TrendDirection.Up, revenue.Trend);
        Assert.Equal(100m, revenue.ChangePercent);
    }

    [Fact]
    public async Task GetKpisAsync_PayrollKpi_ComparesCurrentMonthVsPriorMonth()
    {
        IReadOnlyList<AppHr.PayrollSummaryDto> summaries =
        [
            new("p1", "e1", "Jordan Lee", Now.Month, Now.Year, 3000m, 0m, 0m, 0m, 3000m, Now),
            new("p2", "e1", "Jordan Lee", Now.AddMonths(-1).Month, Now.AddMonths(-1).Year, 3000m, 0m, 0m, 500m, 2500m, Now),
        ];
        var sut = CreateSut([], payrollSummaries: summaries);

        var kpis = await sut.GetKpisAsync(AnalyticsPeriod.Monthly);

        var payroll = kpis.Single(k => k.KpiType == KpiType.Payroll);
        Assert.Equal(3000m, payroll.Value);
        Assert.Equal(2500m, payroll.PreviousValue);
        Assert.Equal(TrendDirection.Up, payroll.Trend);
    }

    [Fact]
    public async Task GetKpisAsync_InventoryKpi_HasFlatTrendSinceNoHistoricalSnapshotExists()
    {
        var sut = CreateSut([]);

        var kpis = await sut.GetKpisAsync(AnalyticsPeriod.Daily);

        var inventory = kpis.Single(k => k.KpiType == KpiType.Inventory);
        Assert.Equal(TrendDirection.Flat, inventory.Trend);
        Assert.Equal(inventory.Value, inventory.PreviousValue);
    }

    [Fact]
    public async Task GetKpisAsync_GrowthKpi_ComparesNewCustomersAcrossPeriods()
    {
        IReadOnlyList<AppCustomers.CustomerDto> customers =
        [
            new("c1", "Alice", string.Empty, "a@x.com", string.Empty, AppCustomers.CustomerStatus.Active, "$0", Now, string.Empty, "org-1", "branch-1"),
        ];
        var sut = CreateSut([], customers: customers);

        var kpis = await sut.GetKpisAsync(AnalyticsPeriod.Daily);

        var growth = kpis.Single(k => k.KpiType == KpiType.Growth);
        Assert.Equal(1m, growth.Value);
        Assert.Equal(0m, growth.PreviousValue);
    }
}
