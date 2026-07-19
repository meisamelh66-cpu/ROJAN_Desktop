namespace Rojan.Desktop.Domain.Reporting;

/// <summary>
/// One computed KPI reading for a period, produced by
/// <c>Application.Reporting.IKpiEngineQueryService</c>. Uses
/// <c>decimal</c> (not the display-string convention most read-only
/// modules use) because <see cref="TrendCalculator"/> needs real
/// arithmetic to derive <see cref="Trend"/>/<see cref="ChangePercent"/> -
/// same justified departure <c>Domain.HR.Employee</c> and
/// <c>Domain.Accounting.Invoice</c> already made for the same reason.
/// </summary>
public sealed record KpiValue(
    string KpiDefinitionId,
    string Name,
    KpiType KpiType,
    decimal Value,
    decimal PreviousValue,
    TrendDirection Trend,
    decimal ChangePercent,
    string Unit,
    string PeriodLabel);
