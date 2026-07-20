using System.Globalization;
using AppReporting = Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Application.AI;

public sealed class ContextProvider : IContextProvider
{
    private readonly AppReporting.IAnalyticsQueryService _analyticsQueryService;

    public ContextProvider(AppReporting.IAnalyticsQueryService analyticsQueryService)
    {
        _analyticsQueryService = analyticsQueryService;
    }

    public async Task<string> GetBusinessContextAsync(CancellationToken cancellationToken = default)
    {
        var summary = await _analyticsQueryService.GetAnalyticsSummaryAsync(AppReporting.AnalyticsPeriod.Monthly, cancellationToken).ConfigureAwait(false);

        return string.Create(CultureInfo.InvariantCulture, $"""
            Business snapshot ({summary.PeriodLabel}):
            - Revenue: ${summary.TotalRevenue:N2}
            - Appointments: {summary.TotalAppointments}
            - Customers: {summary.TotalCustomers} total, {summary.NewCustomers} new, {summary.RetentionRatePercent:N1}% retention
            - Top service: {summary.TopServiceName}; top specialist: {summary.TopSpecialistName}
            - Inventory value: ${summary.InventoryValue:N2}; {summary.LowStockCount} item(s) low on stock
            - Payroll: ${summary.PayrollTotal:N2}; attendance rate: {summary.AttendanceRatePercent:N1}%
            """);
    }
}
