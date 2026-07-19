namespace Rojan.Desktop.Application.Reporting;

public sealed record ChartDefinitionDto(
    string Id,
    string Title,
    ChartType ChartType,
    IReadOnlyList<string> Categories,
    IReadOnlyList<ChartSeriesDto> Series);
