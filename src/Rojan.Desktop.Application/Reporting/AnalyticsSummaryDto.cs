namespace Rojan.Desktop.Application.Reporting;

public sealed record AnalyticsSummaryDto(
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
