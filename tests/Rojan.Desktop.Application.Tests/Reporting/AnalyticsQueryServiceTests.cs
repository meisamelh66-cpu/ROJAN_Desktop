using Rojan.Desktop.Application.Reporting;
using AppAccounting = Rojan.Desktop.Application.Accounting;
using AppBookings = Rojan.Desktop.Application.Bookings;
using AppHr = Rojan.Desktop.Application.HR;

namespace Rojan.Desktop.Application.Tests.Reporting;

public sealed class AnalyticsQueryServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Now;

    private static AnalyticsQueryService CreateSut(
        IReadOnlyList<AppAccounting.InvoiceDto>? invoices = null,
        IReadOnlyList<AppBookings.BookingDto>? bookings = null) => new(
        new StubCustomerQueryService([]),
        new StubBookingQueryService(bookings ?? []),
        new StubProductQueryService([]),
        new StubInventoryQueryService([]),
        new StubInvoiceQueryService(invoices ?? []),
        new StubEmployeeQueryService([]),
        new StubAttendanceQueryService(new Dictionary<string, IReadOnlyList<AppHr.AttendanceDto>>()),
        new StubPayrollQueryService([]));

    [Fact]
    public async Task GetAnalyticsSummaryAsync_ReturnsPeriodLabelMatchingPeriod()
    {
        var sut = CreateSut();

        var daily = await sut.GetAnalyticsSummaryAsync(AnalyticsPeriod.Daily);
        var weekly = await sut.GetAnalyticsSummaryAsync(AnalyticsPeriod.Weekly);
        var monthly = await sut.GetAnalyticsSummaryAsync(AnalyticsPeriod.Monthly);

        Assert.Equal("Today", daily.PeriodLabel);
        Assert.Equal("This Week", weekly.PeriodLabel);
        Assert.Equal("This Month", monthly.PeriodLabel);
    }

    [Fact]
    public async Task GetDashboardChartsAsync_ReturnsExactlyThreeCharts()
    {
        var sut = CreateSut();

        var charts = await sut.GetDashboardChartsAsync(AnalyticsPeriod.Daily);

        Assert.Equal(3, charts.Count);
        Assert.Contains(charts, c => c.Id == "chart-revenue-by-day");
        Assert.Contains(charts, c => c.Id == "chart-appointments-by-status");
        Assert.Contains(charts, c => c.Id == "chart-top-services");
    }

    [Fact]
    public async Task GetDashboardChartsAsync_RevenueByDayChart_HasSevenCategoriesSpanningLastWeek()
    {
        var sut = CreateSut();

        var charts = await sut.GetDashboardChartsAsync(AnalyticsPeriod.Daily);

        var revenueChart = charts.Single(c => c.Id == "chart-revenue-by-day");
        Assert.Equal(7, revenueChart.Categories.Count);
        Assert.Single(revenueChart.Series);
        Assert.Equal(7, revenueChart.Series[0].Values.Count);
    }

    [Fact]
    public async Task GetDashboardChartsAsync_RevenueByDayChart_SumsInvoicesFallingOnEachDay()
    {
        IReadOnlyList<AppAccounting.InvoiceDto> invoices =
        [
            new("invoice-1", "c1", "Alice", "b1", "BK-1", Now, AppAccounting.InvoiceStatus.Paid, 100m, 0m, 150m, string.Empty),
            new("invoice-2", "c1", "Alice", "b2", "BK-2", Now, AppAccounting.InvoiceStatus.Paid, 50m, 0m, 75m, string.Empty),
        ];
        var sut = CreateSut(invoices: invoices);

        var charts = await sut.GetDashboardChartsAsync(AnalyticsPeriod.Daily);

        var revenueChart = charts.Single(c => c.Id == "chart-revenue-by-day");
        Assert.Equal(225m, revenueChart.Series[0].Values[^1]);
    }

    [Fact]
    public async Task GetDashboardChartsAsync_AppointmentsByStatusChart_GroupsBookingsByStatus()
    {
        IReadOnlyList<AppBookings.BookingDto> bookings =
        [
            new("b1", "c1", "Alice", "s1", "Haircut", "sp1", "Jordan", Now, 60, "$65", AppBookings.BookingStatus.Completed, string.Empty),
            new("b2", "c1", "Alice", "s1", "Haircut", "sp1", "Jordan", Now, 60, "$65", AppBookings.BookingStatus.Completed, string.Empty),
            new("b3", "c1", "Alice", "s1", "Haircut", "sp1", "Jordan", Now, 60, "$65", AppBookings.BookingStatus.Cancelled, string.Empty),
        ];
        var sut = CreateSut(bookings: bookings);

        var charts = await sut.GetDashboardChartsAsync(AnalyticsPeriod.Daily);

        var statusChart = charts.Single(c => c.Id == "chart-appointments-by-status");
        Assert.Contains("Completed", statusChart.Categories);
        var completedIndex = statusChart.Categories.ToList().IndexOf("Completed");
        Assert.Equal(2m, statusChart.Series[0].Values[completedIndex]);
    }

    [Fact]
    public async Task GetDashboardChartsAsync_TopServicesChart_ReturnsAtMostFiveServices()
    {
        var bookings = Enumerable.Range(1, 8)
            .Select(i => new AppBookings.BookingDto($"b{i}", "c1", "Alice", $"s{i}", $"Service {i}", "sp1", "Jordan", Now, 60, "$65", AppBookings.BookingStatus.Completed, string.Empty))
            .ToList();
        var sut = CreateSut(bookings: bookings);

        var charts = await sut.GetDashboardChartsAsync(AnalyticsPeriod.Daily);

        var topServicesChart = charts.Single(c => c.Id == "chart-top-services");
        Assert.True(topServicesChart.Categories.Count <= 5);
    }
}
