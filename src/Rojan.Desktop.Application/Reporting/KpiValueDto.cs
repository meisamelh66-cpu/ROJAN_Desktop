namespace Rojan.Desktop.Application.Reporting;

public sealed record KpiValueDto(
    string KpiDefinitionId,
    string Name,
    KpiType KpiType,
    decimal Value,
    decimal PreviousValue,
    TrendDirection Trend,
    decimal ChangePercent,
    string Unit,
    string PeriodLabel);
