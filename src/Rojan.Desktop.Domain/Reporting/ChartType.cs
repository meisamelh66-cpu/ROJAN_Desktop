namespace Rojan.Desktop.Domain.Reporting;

/// <summary>Chart shapes the Analytics platform can describe - Presentation renders these without any external charting library (placeholder/native rendering only).</summary>
public enum ChartType
{
    Line,
    Bar,
    Pie,
    Area,
    Column,

    // Phase 33: Enterprise Reporting & Business Intelligence Platform.
    Donut,
    Trend,

    /// <summary>Architecture-ready only, per Phase 33's own scope - no renderer exists yet, same "contract without a build-out" boundary Phase 32 set for Database/API workflow steps.</summary>
    HeatMap,
}
