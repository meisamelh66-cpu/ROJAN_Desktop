using DomainReporting = Rojan.Desktop.Domain.Reporting;

namespace Rojan.Desktop.Application.Tests.Reporting;

/// <summary>
/// A minimal <see cref="DomainReporting.IReportingRepository"/> for
/// Application-layer tests - defined here rather than reusing
/// <c>Infrastructure.Reporting.FakeReportingRepository</c> because
/// <c>Rojan.Desktop.Application.Tests</c> deliberately does not reference
/// <c>Rojan.Desktop.Infrastructure</c> (Application never depends on
/// Infrastructure - see <c>ArchitectureTests</c>). Only carries the
/// catalog entries these tests actually run reports against;
/// <see cref="Rojan.Desktop.Application.Reporting.ReportExecutionQueryService"/>
/// only reads <c>Id</c>/<c>Name</c>/<c>ReportType</c>/<c>Columns</c> from a
/// definition, so this intentionally omits <c>Description</c>/<c>Category</c>/
/// <c>SupportedFilters</c> detail beyond what each report actually needs.
/// </summary>
internal sealed class StubReportingRepository : DomainReporting.IReportingRepository
{
    private static readonly IReadOnlyList<DomainReporting.ReportDefinition> Definitions =
    [
        Build("revenue-report", "Revenue Report", DomainReporting.ReportType.RevenueReport,
            ("date", "Date", DomainReporting.ReportColumnDataType.Date),
            ("invoiceCount", "Invoices", DomainReporting.ReportColumnDataType.Number),
            ("totalRevenue", "Total Revenue", DomainReporting.ReportColumnDataType.Currency)),
        Build("sales-report", "Sales Report", DomainReporting.ReportType.SalesReport,
            ("invoiceId", "Invoice", DomainReporting.ReportColumnDataType.Text),
            ("customerName", "Customer", DomainReporting.ReportColumnDataType.Text),
            ("issuedAt", "Issued", DomainReporting.ReportColumnDataType.Date),
            ("status", "Status", DomainReporting.ReportColumnDataType.Text),
            ("total", "Total", DomainReporting.ReportColumnDataType.Currency)),
        Build("appointments-report", "Appointments Report", DomainReporting.ReportType.AppointmentsReport,
            ("date", "Date", DomainReporting.ReportColumnDataType.Date),
            ("customerName", "Customer", DomainReporting.ReportColumnDataType.Text),
            ("serviceName", "Service", DomainReporting.ReportColumnDataType.Text),
            ("specialistName", "Specialist", DomainReporting.ReportColumnDataType.Text),
            ("status", "Status", DomainReporting.ReportColumnDataType.Text),
            ("price", "Price", DomainReporting.ReportColumnDataType.Currency)),
        Build("customer-growth", "Customer Growth", DomainReporting.ReportType.CustomerGrowth,
            ("period", "Period", DomainReporting.ReportColumnDataType.Text),
            ("newCustomers", "New Customers", DomainReporting.ReportColumnDataType.Number),
            ("totalCustomers", "Total Customers", DomainReporting.ReportColumnDataType.Number)),
        Build("customer-retention", "Customer Retention", DomainReporting.ReportType.CustomerRetention,
            ("status", "Status", DomainReporting.ReportColumnDataType.Text),
            ("count", "Count", DomainReporting.ReportColumnDataType.Number),
            ("percentage", "Share", DomainReporting.ReportColumnDataType.Percentage)),
        Build("service-popularity", "Service Popularity", DomainReporting.ReportType.ServicePopularity,
            ("serviceName", "Service", DomainReporting.ReportColumnDataType.Text),
            ("bookingCount", "Bookings", DomainReporting.ReportColumnDataType.Number),
            ("revenue", "Revenue", DomainReporting.ReportColumnDataType.Currency)),
        Build("specialist-performance", "Specialist Performance", DomainReporting.ReportType.SpecialistPerformance,
            ("specialistName", "Specialist", DomainReporting.ReportColumnDataType.Text),
            ("bookingCount", "Bookings", DomainReporting.ReportColumnDataType.Number),
            ("revenue", "Revenue", DomainReporting.ReportColumnDataType.Currency),
            ("commissionEarned", "Commission Earned", DomainReporting.ReportColumnDataType.Currency)),
        Build("inventory-valuation", "Inventory Valuation", DomainReporting.ReportType.InventoryValuation,
            ("productName", "Product", DomainReporting.ReportColumnDataType.Text),
            ("quantityOnHand", "Qty On Hand", DomainReporting.ReportColumnDataType.Number),
            ("unitPrice", "Unit Price", DomainReporting.ReportColumnDataType.Currency),
            ("totalValue", "Total Value", DomainReporting.ReportColumnDataType.Currency)),
        Build("low-stock", "Low Stock", DomainReporting.ReportType.LowStock,
            ("productName", "Product", DomainReporting.ReportColumnDataType.Text),
            ("quantityOnHand", "Qty On Hand", DomainReporting.ReportColumnDataType.Number),
            ("reorderThreshold", "Reorder Threshold", DomainReporting.ReportColumnDataType.Number),
            ("shortfall", "Shortfall", DomainReporting.ReportColumnDataType.Number)),
        Build("payroll-summary", "Payroll Summary", DomainReporting.ReportType.PayrollSummary,
            ("employeeName", "Employee", DomainReporting.ReportColumnDataType.Text),
            ("period", "Period", DomainReporting.ReportColumnDataType.Text),
            ("baseSalary", "Base Salary", DomainReporting.ReportColumnDataType.Currency),
            ("commissionTotal", "Commission", DomainReporting.ReportColumnDataType.Currency),
            ("bonus", "Bonus", DomainReporting.ReportColumnDataType.Currency),
            ("deduction", "Deduction", DomainReporting.ReportColumnDataType.Currency),
            ("netSalary", "Net Salary", DomainReporting.ReportColumnDataType.Currency)),
        Build("commission-summary", "Commission Summary", DomainReporting.ReportType.CommissionSummary,
            ("employeeName", "Employee", DomainReporting.ReportColumnDataType.Text),
            ("transactionCount", "Transactions", DomainReporting.ReportColumnDataType.Number),
            ("totalCommission", "Total Commission", DomainReporting.ReportColumnDataType.Currency)),
        Build("attendance-summary", "Attendance Summary", DomainReporting.ReportType.AttendanceSummary,
            ("employeeName", "Employee", DomainReporting.ReportColumnDataType.Text),
            ("presentCount", "Present", DomainReporting.ReportColumnDataType.Number),
            ("lateCount", "Late", DomainReporting.ReportColumnDataType.Number),
            ("absentCount", "Absent", DomainReporting.ReportColumnDataType.Number),
            ("attendanceRate", "Attendance Rate", DomainReporting.ReportColumnDataType.Percentage)),
        Build("daily-dashboard", "Daily Dashboard", DomainReporting.ReportType.DailyDashboard,
            ("metric", "Metric", DomainReporting.ReportColumnDataType.Text),
            ("value", "Value", DomainReporting.ReportColumnDataType.Text)),
        Build("weekly-dashboard", "Weekly Dashboard", DomainReporting.ReportType.WeeklyDashboard,
            ("metric", "Metric", DomainReporting.ReportColumnDataType.Text),
            ("value", "Value", DomainReporting.ReportColumnDataType.Text)),
        Build("monthly-dashboard", "Monthly Dashboard", DomainReporting.ReportType.MonthlyDashboard,
            ("metric", "Metric", DomainReporting.ReportColumnDataType.Text),
            ("value", "Value", DomainReporting.ReportColumnDataType.Text)),
        Build("cash-flow", "Cash Flow", DomainReporting.ReportType.CashFlow,
            ("date", "Date", DomainReporting.ReportColumnDataType.Date),
            ("cashIn", "Cash In", DomainReporting.ReportColumnDataType.Currency)),
        Build("outstanding-payments", "Outstanding Payments", DomainReporting.ReportType.OutstandingPayments,
            ("invoiceId", "Invoice", DomainReporting.ReportColumnDataType.Text),
            ("customerName", "Customer", DomainReporting.ReportColumnDataType.Text),
            ("issuedAt", "Issued", DomainReporting.ReportColumnDataType.Date),
            ("status", "Status", DomainReporting.ReportColumnDataType.Text),
            ("outstanding", "Outstanding", DomainReporting.ReportColumnDataType.Currency)),
        Build("tax-summary", "Tax Summary", DomainReporting.ReportType.TaxSummary,
            ("period", "Period", DomainReporting.ReportColumnDataType.Text),
            ("taxableAmount", "Taxable Amount", DomainReporting.ReportColumnDataType.Currency),
            ("taxCollected", "Tax Collected", DomainReporting.ReportColumnDataType.Currency)),
        Build("vip-customers", "VIP Customers", DomainReporting.ReportType.VipCustomers,
            ("name", "Name", DomainReporting.ReportColumnDataType.Text),
            ("company", "Company", DomainReporting.ReportColumnDataType.Text),
            ("lifetimeValue", "Lifetime Value", DomainReporting.ReportColumnDataType.Currency),
            ("lastContacted", "Last Contacted", DomainReporting.ReportColumnDataType.Date)),
        Build("inactive-customers", "Inactive Customers", DomainReporting.ReportType.InactiveCustomers,
            ("name", "Name", DomainReporting.ReportColumnDataType.Text),
            ("lastContacted", "Last Contacted", DomainReporting.ReportColumnDataType.Date),
            ("phone", "Phone", DomainReporting.ReportColumnDataType.Text)),
        Build("customer-lifetime-value", "Customer Lifetime Value", DomainReporting.ReportType.CustomerLifetimeValue,
            ("name", "Name", DomainReporting.ReportColumnDataType.Text),
            ("status", "Status", DomainReporting.ReportColumnDataType.Text),
            ("lifetimeValue", "Lifetime Value", DomainReporting.ReportColumnDataType.Currency)),
        Build("appointment-status-breakdown", "Appointment Status Breakdown", DomainReporting.ReportType.AppointmentStatusBreakdown,
            ("status", "Status", DomainReporting.ReportColumnDataType.Text),
            ("count", "Count", DomainReporting.ReportColumnDataType.Number),
            ("percentage", "Share", DomainReporting.ReportColumnDataType.Percentage)),
        Build("peak-hours", "Peak Hours", DomainReporting.ReportType.PeakHours,
            ("hour", "Hour", DomainReporting.ReportColumnDataType.Text),
            ("bookingCount", "Bookings", DomainReporting.ReportColumnDataType.Number)),
        Build("inventory-movements", "Inventory Movements", DomainReporting.ReportType.InventoryMovements,
            ("date", "Date", DomainReporting.ReportColumnDataType.Date),
            ("productName", "Product", DomainReporting.ReportColumnDataType.Text),
            ("type", "Type", DomainReporting.ReportColumnDataType.Text),
            ("quantity", "Quantity", DomainReporting.ReportColumnDataType.Number)),
        Build("supplier-purchases", "Supplier Purchases", DomainReporting.ReportType.SupplierPurchases,
            ("supplierName", "Supplier", DomainReporting.ReportColumnDataType.Text),
            ("transactionCount", "Transactions", DomainReporting.ReportColumnDataType.Number),
            ("totalQuantity", "Total Quantity", DomainReporting.ReportColumnDataType.Number)),
        Build("employee-working-hours", "Employee Working Hours", DomainReporting.ReportType.EmployeeWorkingHours,
            ("employeeName", "Employee", DomainReporting.ReportColumnDataType.Text),
            ("shiftCount", "Shifts", DomainReporting.ReportColumnDataType.Number),
            ("totalHours", "Total Hours", DomainReporting.ReportColumnDataType.Number)),
        Build("branch-performance", "Branch Performance", DomainReporting.ReportType.BranchPerformance,
            ("branchName", "Branch", DomainReporting.ReportColumnDataType.Text),
            ("bookingCount", "Bookings", DomainReporting.ReportColumnDataType.Number),
            ("revenue", "Revenue", DomainReporting.ReportColumnDataType.Currency)),
        Build("ai-usage-summary", "AI Usage Summary", DomainReporting.ReportType.AiUsageSummary,
            ("provider", "Provider", DomainReporting.ReportColumnDataType.Text),
            ("sessionCount", "Sessions", DomainReporting.ReportColumnDataType.Number),
            ("totalTokens", "Total Tokens", DomainReporting.ReportColumnDataType.Number)),
    ];

    private readonly List<DomainReporting.ReportSnapshot> _snapshots = [];

    public Task<IReadOnlyList<DomainReporting.ReportDefinition>> GetReportDefinitionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Definitions);

    public Task<DomainReporting.ReportDefinition?> GetReportDefinitionByIdAsync(string reportDefinitionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Definitions.FirstOrDefault(d => d.Id == reportDefinitionId));

    public Task<IReadOnlyList<DomainReporting.ReportSnapshot>> GetSnapshotsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DomainReporting.ReportSnapshot>>(_snapshots.OrderByDescending(s => s.GeneratedAt).ToList());

    public Task<DomainReporting.ReportSnapshot> CreateSnapshotAsync(DomainReporting.ReportSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        _snapshots.Add(snapshot);
        return Task.FromResult(snapshot);
    }

    public Task<DomainReporting.ReportSnapshot> UpdateSnapshotSavedStateAsync(string snapshotId, bool isSaved, CancellationToken cancellationToken = default)
    {
        var index = _snapshots.FindIndex(s => s.Id == snapshotId);
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
        _snapshots.RemoveAll(s => s.Id == snapshotId);
        return Task.CompletedTask;
    }

    private static DomainReporting.ReportDefinition Build(
        string id, string name, DomainReporting.ReportType reportType, params (string Key, string Header, DomainReporting.ReportColumnDataType DataType)[] columns) =>
        new(
            id,
            name,
            $"{name} description.",
            reportType,
            DomainReporting.ReportCategory.Operations,
            true,
            columns.Select(c => new DomainReporting.ReportColumn(c.Key, c.Header, c.DataType)).ToList(),
            []);
}
