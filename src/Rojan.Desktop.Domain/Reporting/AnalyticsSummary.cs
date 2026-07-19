namespace Rojan.Desktop.Domain.Reporting;

/// <summary>
/// The Analytics Dashboard's single-glance rollup for a period (Daily/
/// Weekly/Monthly) - one aggregate composed from every module's own
/// Application-layer query services, computed fresh on every request
/// (nothing here is persisted; see <see cref="ReportSnapshot"/> for the
/// one thing Reporting does persist).
/// </summary>
public sealed record AnalyticsSummary(
    string PeriodLabel,
    decimal TotalRevenue,
    int TotalAppointments,
    int TotalCustomers,
    int NewCustomers,
    decimal RetentionRatePercent,
    string TopServiceName,
    string TopSpecialistName,
    decimal InventoryValue,
    int LowStockCount,
    decimal PayrollTotal,
    decimal AttendanceRatePercent,
    DateTimeOffset GeneratedAt);
