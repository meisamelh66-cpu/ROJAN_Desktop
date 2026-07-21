using Rojan.Desktop.Domain.Reporting;
using Rojan.Desktop.Infrastructure.Reporting;

namespace Rojan.Desktop.Infrastructure.Tests.Reporting;

public sealed class FakeReportingRepositoryTests
{
    [Fact]
    public async Task GetReportDefinitionsAsync_ReturnsAllTwentyEightSeededReports()
    {
        var repository = new FakeReportingRepository();

        var definitions = await repository.GetReportDefinitionsAsync();

        Assert.Equal(28, definitions.Count);
        Assert.All(definitions, definition => Assert.True(definition.IsSystemDefined));
    }

    [Theory]
    [InlineData("revenue-report", ReportType.RevenueReport)]
    [InlineData("sales-report", ReportType.SalesReport)]
    [InlineData("appointments-report", ReportType.AppointmentsReport)]
    [InlineData("customer-growth", ReportType.CustomerGrowth)]
    [InlineData("customer-retention", ReportType.CustomerRetention)]
    [InlineData("service-popularity", ReportType.ServicePopularity)]
    [InlineData("specialist-performance", ReportType.SpecialistPerformance)]
    [InlineData("inventory-valuation", ReportType.InventoryValuation)]
    [InlineData("low-stock", ReportType.LowStock)]
    [InlineData("payroll-summary", ReportType.PayrollSummary)]
    [InlineData("commission-summary", ReportType.CommissionSummary)]
    [InlineData("attendance-summary", ReportType.AttendanceSummary)]
    [InlineData("daily-dashboard", ReportType.DailyDashboard)]
    [InlineData("weekly-dashboard", ReportType.WeeklyDashboard)]
    [InlineData("monthly-dashboard", ReportType.MonthlyDashboard)]
    [InlineData("cash-flow", ReportType.CashFlow)]
    [InlineData("outstanding-payments", ReportType.OutstandingPayments)]
    [InlineData("tax-summary", ReportType.TaxSummary)]
    [InlineData("vip-customers", ReportType.VipCustomers)]
    [InlineData("inactive-customers", ReportType.InactiveCustomers)]
    [InlineData("customer-lifetime-value", ReportType.CustomerLifetimeValue)]
    [InlineData("appointment-status-breakdown", ReportType.AppointmentStatusBreakdown)]
    [InlineData("peak-hours", ReportType.PeakHours)]
    [InlineData("inventory-movements", ReportType.InventoryMovements)]
    [InlineData("supplier-purchases", ReportType.SupplierPurchases)]
    [InlineData("employee-working-hours", ReportType.EmployeeWorkingHours)]
    [InlineData("branch-performance", ReportType.BranchPerformance)]
    [InlineData("ai-usage-summary", ReportType.AiUsageSummary)]
    public async Task GetReportDefinitionByIdAsync_EachSeededReport_HasExpectedReportType(string id, ReportType expectedType)
    {
        var repository = new FakeReportingRepository();

        var definition = await repository.GetReportDefinitionByIdAsync(id);

        Assert.NotNull(definition);
        Assert.Equal(expectedType, definition.ReportType);
    }

    [Fact]
    public async Task GetReportDefinitionByIdAsync_WithUnknownId_ReturnsNull()
    {
        var repository = new FakeReportingRepository();

        var definition = await repository.GetReportDefinitionByIdAsync("does-not-exist");

        Assert.Null(definition);
    }

    [Fact]
    public async Task GetSnapshotsAsync_ReturnsSeededSnapshotsNewestFirst()
    {
        var repository = new FakeReportingRepository();

        var snapshots = await repository.GetSnapshotsAsync();

        Assert.NotEmpty(snapshots);
        Assert.Equal(snapshots.OrderByDescending(s => s.GeneratedAt).Select(s => s.Id), snapshots.Select(s => s.Id));
    }

    [Fact]
    public async Task GetSnapshotsAsync_IncludesBothSavedAndUnsavedSnapshots()
    {
        var repository = new FakeReportingRepository();

        var snapshots = await repository.GetSnapshotsAsync();

        Assert.Contains(snapshots, s => s.IsSaved);
        Assert.Contains(snapshots, s => !s.IsSaved);
    }

    [Fact]
    public async Task CreateSnapshotAsync_AddsSnapshotToSubsequentReads()
    {
        var repository = new FakeReportingRepository();
        var newSnapshot = new ReportSnapshot("snapshot-new", "revenue-report", "Revenue Report", DateTimeOffset.Now, [], 3, false);

        await repository.CreateSnapshotAsync(newSnapshot);
        var snapshots = await repository.GetSnapshotsAsync();

        Assert.Contains(snapshots, s => s.Id == "snapshot-new");
    }

    [Fact]
    public async Task UpdateSnapshotSavedStateAsync_TogglesIsSaved()
    {
        var repository = new FakeReportingRepository();
        var snapshots = await repository.GetSnapshotsAsync();
        var target = snapshots.First(s => !s.IsSaved);

        var updated = await repository.UpdateSnapshotSavedStateAsync(target.Id, true);

        Assert.True(updated.IsSaved);
        Assert.Equal(target.Id, updated.Id);
    }

    [Fact]
    public async Task UpdateSnapshotSavedStateAsync_WithUnknownId_Throws()
    {
        var repository = new FakeReportingRepository();

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateSnapshotSavedStateAsync("does-not-exist", true));
    }

    [Fact]
    public async Task DeleteSnapshotAsync_RemovesSnapshotFromSubsequentReads()
    {
        var repository = new FakeReportingRepository();
        var snapshots = await repository.GetSnapshotsAsync();
        var target = snapshots[0];

        await repository.DeleteSnapshotAsync(target.Id);
        var remaining = await repository.GetSnapshotsAsync();

        Assert.DoesNotContain(remaining, s => s.Id == target.Id);
    }
}
