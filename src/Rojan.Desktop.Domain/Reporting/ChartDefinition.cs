namespace Rojan.Desktop.Domain.Reporting;

/// <summary>
/// Describes one chart the Analytics Dashboard renders. Deliberately just
/// data - no external charting library is used anywhere in this app;
/// Presentation renders <see cref="ChartType"/> as a native, proportional
/// placeholder visual (e.g. sized bars/columns) built entirely from
/// <see cref="Categories"/>/<see cref="Series"/>.
/// </summary>
public sealed record ChartDefinition(
    string Id,
    string Title,
    ChartType ChartType,
    IReadOnlyList<string> Categories,
    IReadOnlyList<ChartSeries> Series);
