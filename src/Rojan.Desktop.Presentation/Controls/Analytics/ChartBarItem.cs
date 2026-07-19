namespace Rojan.Desktop.Presentation.Controls.Analytics;

/// <summary>One rendered bar of a <see cref="SimpleBarChart"/> - <see cref="BarWidth"/> is pre-computed in code-behind (proportional to the chart's own maximum value), not via a converter, since it depends on the whole series, not just one value.</summary>
public sealed class ChartBarItem
{
    public required string Label { get; init; }

    public required string DisplayValue { get; init; }

    public required double BarWidth { get; init; }
}
