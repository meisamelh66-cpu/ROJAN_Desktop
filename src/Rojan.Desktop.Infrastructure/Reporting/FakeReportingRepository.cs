using Rojan.Desktop.Domain.Reporting;

namespace Rojan.Desktop.Infrastructure.Reporting;

/// <summary>
/// In-memory <see cref="IReportingRepository"/>. Seeds the fifteen
/// system-defined <see cref="ReportDefinition"/>s (the report catalog) plus
/// a handful of <see cref="ReportSnapshot"/>s so "Saved Reports"/"Recent
/// Reports" have something to show on first launch. Registered as a
/// singleton (same reasoning as every other Fake repository with real
/// writes - <c>FakeHrRepository</c>, <c>FakeAccountingRepository</c> - it
/// must remember snapshot writes for the app's lifetime).
/// </summary>
public sealed class FakeReportingRepository : IReportingRepository
{
    private readonly List<ReportDefinition> _reportDefinitions;
    private readonly List<ReportSnapshot> _snapshots;

    public FakeReportingRepository()
    {
        _reportDefinitions =
        [
            new ReportDefinition(
                "revenue-report", "Revenue Report", "Daily revenue totals from Accounting invoices.",
                ReportType.RevenueReport, ReportCategory.Financial, true,
                [
                    new ReportColumn("date", "Date", ReportColumnDataType.Date),
                    new ReportColumn("invoiceCount", "Invoices", ReportColumnDataType.Number),
                    new ReportColumn("totalRevenue", "Total Revenue", ReportColumnDataType.Currency),
                ],
                [FilterType.DateRange]),
            new ReportDefinition(
                "sales-report", "Sales Report", "Every invoice issued, with customer and status.",
                ReportType.SalesReport, ReportCategory.Financial, true,
                [
                    new ReportColumn("invoiceId", "Invoice", ReportColumnDataType.Text),
                    new ReportColumn("customerName", "Customer", ReportColumnDataType.Text),
                    new ReportColumn("issuedAt", "Issued", ReportColumnDataType.Date),
                    new ReportColumn("status", "Status", ReportColumnDataType.Text),
                    new ReportColumn("total", "Total", ReportColumnDataType.Currency),
                ],
                [FilterType.DateRange, FilterType.Customer, FilterType.Status]),
            new ReportDefinition(
                "appointments-report", "Appointments Report", "Every booking, with service, specialist, and status.",
                ReportType.AppointmentsReport, ReportCategory.Operations, true,
                [
                    new ReportColumn("date", "Date", ReportColumnDataType.Date),
                    new ReportColumn("customerName", "Customer", ReportColumnDataType.Text),
                    new ReportColumn("serviceName", "Service", ReportColumnDataType.Text),
                    new ReportColumn("specialistName", "Specialist", ReportColumnDataType.Text),
                    new ReportColumn("status", "Status", ReportColumnDataType.Text),
                    new ReportColumn("price", "Price", ReportColumnDataType.Currency),
                ],
                [FilterType.DateRange, FilterType.Specialist, FilterType.Service, FilterType.Status, FilterType.Customer]),
            new ReportDefinition(
                "customer-growth", "Customer Growth", "New customers by month, based on last-contacted date.",
                ReportType.CustomerGrowth, ReportCategory.Customers, true,
                [
                    new ReportColumn("period", "Period", ReportColumnDataType.Text),
                    new ReportColumn("newCustomers", "New Customers", ReportColumnDataType.Number),
                    new ReportColumn("totalCustomers", "Total Customers", ReportColumnDataType.Number),
                ],
                [FilterType.DateRange]),
            new ReportDefinition(
                "customer-retention", "Customer Retention", "Customer count and share by relationship status.",
                ReportType.CustomerRetention, ReportCategory.Customers, true,
                [
                    new ReportColumn("status", "Status", ReportColumnDataType.Text),
                    new ReportColumn("count", "Count", ReportColumnDataType.Number),
                    new ReportColumn("percentage", "Share", ReportColumnDataType.Percentage),
                ],
                [FilterType.Status]),
            new ReportDefinition(
                "service-popularity", "Service Popularity", "Booking count and revenue by service.",
                ReportType.ServicePopularity, ReportCategory.Operations, true,
                [
                    new ReportColumn("serviceName", "Service", ReportColumnDataType.Text),
                    new ReportColumn("bookingCount", "Bookings", ReportColumnDataType.Number),
                    new ReportColumn("revenue", "Revenue", ReportColumnDataType.Currency),
                ],
                [FilterType.DateRange, FilterType.Service, FilterType.Category]),
            new ReportDefinition(
                "specialist-performance", "Specialist Performance", "Bookings, revenue, and commission earned per specialist.",
                ReportType.SpecialistPerformance, ReportCategory.Workforce, true,
                [
                    new ReportColumn("specialistName", "Specialist", ReportColumnDataType.Text),
                    new ReportColumn("bookingCount", "Bookings", ReportColumnDataType.Number),
                    new ReportColumn("revenue", "Revenue", ReportColumnDataType.Currency),
                    new ReportColumn("commissionEarned", "Commission Earned", ReportColumnDataType.Currency),
                ],
                [FilterType.DateRange, FilterType.Specialist]),
            new ReportDefinition(
                "inventory-valuation", "Inventory Valuation", "On-hand quantity and value by product.",
                ReportType.InventoryValuation, ReportCategory.Operations, true,
                [
                    new ReportColumn("productName", "Product", ReportColumnDataType.Text),
                    new ReportColumn("quantityOnHand", "Qty On Hand", ReportColumnDataType.Number),
                    new ReportColumn("unitPrice", "Unit Price", ReportColumnDataType.Currency),
                    new ReportColumn("totalValue", "Total Value", ReportColumnDataType.Currency),
                ],
                [FilterType.Category]),
            new ReportDefinition(
                "low-stock", "Low Stock", "Products at or below their reorder threshold.",
                ReportType.LowStock, ReportCategory.Operations, true,
                [
                    new ReportColumn("productName", "Product", ReportColumnDataType.Text),
                    new ReportColumn("quantityOnHand", "Qty On Hand", ReportColumnDataType.Number),
                    new ReportColumn("reorderThreshold", "Reorder Threshold", ReportColumnDataType.Number),
                    new ReportColumn("shortfall", "Shortfall", ReportColumnDataType.Number),
                ],
                []),
            new ReportDefinition(
                "payroll-summary", "Payroll Summary", "Base, commission, bonus, deduction, and net pay per employee.",
                ReportType.PayrollSummary, ReportCategory.Workforce, true,
                [
                    new ReportColumn("employeeName", "Employee", ReportColumnDataType.Text),
                    new ReportColumn("period", "Period", ReportColumnDataType.Text),
                    new ReportColumn("baseSalary", "Base Salary", ReportColumnDataType.Currency),
                    new ReportColumn("commissionTotal", "Commission", ReportColumnDataType.Currency),
                    new ReportColumn("bonus", "Bonus", ReportColumnDataType.Currency),
                    new ReportColumn("deduction", "Deduction", ReportColumnDataType.Currency),
                    new ReportColumn("netSalary", "Net Salary", ReportColumnDataType.Currency),
                ],
                [FilterType.Employee, FilterType.DateRange]),
            new ReportDefinition(
                "commission-summary", "Commission Summary", "Commission transactions and totals per employee.",
                ReportType.CommissionSummary, ReportCategory.Workforce, true,
                [
                    new ReportColumn("employeeName", "Employee", ReportColumnDataType.Text),
                    new ReportColumn("transactionCount", "Transactions", ReportColumnDataType.Number),
                    new ReportColumn("totalCommission", "Total Commission", ReportColumnDataType.Currency),
                ],
                [FilterType.Employee, FilterType.DateRange]),
            new ReportDefinition(
                "attendance-summary", "Attendance Summary", "Present/late/absent counts and attendance rate per employee.",
                ReportType.AttendanceSummary, ReportCategory.Workforce, true,
                [
                    new ReportColumn("employeeName", "Employee", ReportColumnDataType.Text),
                    new ReportColumn("presentCount", "Present", ReportColumnDataType.Number),
                    new ReportColumn("lateCount", "Late", ReportColumnDataType.Number),
                    new ReportColumn("absentCount", "Absent", ReportColumnDataType.Number),
                    new ReportColumn("attendanceRate", "Attendance Rate", ReportColumnDataType.Percentage),
                ],
                [FilterType.Employee, FilterType.DateRange]),
            new ReportDefinition(
                "daily-dashboard", "Daily Dashboard", "Today's headline metrics across every module.",
                ReportType.DailyDashboard, ReportCategory.Dashboards, true,
                [
                    new ReportColumn("metric", "Metric", ReportColumnDataType.Text),
                    new ReportColumn("value", "Value", ReportColumnDataType.Text),
                ],
                []),
            new ReportDefinition(
                "weekly-dashboard", "Weekly Dashboard", "This week's headline metrics across every module.",
                ReportType.WeeklyDashboard, ReportCategory.Dashboards, true,
                [
                    new ReportColumn("metric", "Metric", ReportColumnDataType.Text),
                    new ReportColumn("value", "Value", ReportColumnDataType.Text),
                ],
                []),
            new ReportDefinition(
                "monthly-dashboard", "Monthly Dashboard", "This month's headline metrics across every module.",
                ReportType.MonthlyDashboard, ReportCategory.Dashboards, true,
                [
                    new ReportColumn("metric", "Metric", ReportColumnDataType.Text),
                    new ReportColumn("value", "Value", ReportColumnDataType.Text),
                ],
                []),
        ];

        var now = DateTimeOffset.Now;
        _snapshots =
        [
            new ReportSnapshot("snapshot-1", "revenue-report", "Revenue Report", now.AddDays(-1), [], 7, true),
            new ReportSnapshot("snapshot-2", "specialist-performance", "Specialist Performance", now.AddDays(-1).AddHours(-2), [], 5, true),
            new ReportSnapshot("snapshot-3", "low-stock", "Low Stock", now.AddHours(-5), [], 3, false),
            new ReportSnapshot("snapshot-4", "appointments-report", "Appointments Report", now.AddHours(-1), [], 12, false),
        ];
    }

    public Task<IReadOnlyList<ReportDefinition>> GetReportDefinitionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ReportDefinition>>(_reportDefinitions);

    public Task<ReportDefinition?> GetReportDefinitionByIdAsync(string reportDefinitionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_reportDefinitions.FirstOrDefault(definition => definition.Id == reportDefinitionId));

    public Task<IReadOnlyList<ReportSnapshot>> GetSnapshotsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ReportSnapshot>>(_snapshots.OrderByDescending(snapshot => snapshot.GeneratedAt).ToList());

    public Task<ReportSnapshot> CreateSnapshotAsync(ReportSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        _snapshots.Add(snapshot);
        return Task.FromResult(snapshot);
    }

    public Task<ReportSnapshot> UpdateSnapshotSavedStateAsync(string snapshotId, bool isSaved, CancellationToken cancellationToken = default)
    {
        var index = _snapshots.FindIndex(snapshot => snapshot.Id == snapshotId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Snapshot '{snapshotId}' was not found.");
        }

        var updated = _snapshots[index] with { IsSaved = isSaved };
        _snapshots[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteSnapshotAsync(string snapshotId, CancellationToken cancellationToken = default)
    {
        _snapshots.RemoveAll(snapshot => snapshot.Id == snapshotId);
        return Task.CompletedTask;
    }
}
