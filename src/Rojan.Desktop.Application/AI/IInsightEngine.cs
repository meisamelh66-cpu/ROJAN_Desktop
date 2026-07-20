namespace Rojan.Desktop.Application.AI;

/// <summary>
/// Generates <see cref="AIInsightDto"/>s covering every business area
/// (Revenue/Customer/Appointment/Inventory/HR/Payroll/Attendance/
/// Commission) plus Trend/Risk/Opportunity classification - all computed
/// fresh from <c>Reporting.IKpiEngineQueryService</c>/
/// <c>IAnalyticsQueryService</c> (reused unchanged) and HR's commission
/// data, never persisted (see <c>Domain.AI.IAIRepository</c>'s own doc
/// comment for why).
/// </summary>
public interface IInsightEngine
{
    public Task<IReadOnlyList<AIInsightDto>> GenerateInsightsAsync(InsightCategory? filter = null, CancellationToken cancellationToken = default);
}
