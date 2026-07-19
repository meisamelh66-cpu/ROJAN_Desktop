namespace Rojan.Desktop.Domain.Reporting;

/// <summary>Chart shapes the Analytics platform can describe - Presentation renders these without any external charting library (placeholder/native rendering only).</summary>
public enum ChartType
{
    Line,
    Bar,
    Pie,
    Area,
    Column,
}
