namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>
/// Phase 35 (Analytics Expansion): a presentation-only marker for one of the
/// two synthetic "analytics" KPI cards - NOT a ViewModel or DTO, never
/// touches Application/Domain. Placed alongside the four real KpiMetricDto
/// items in DashboardPage's CompositeCollection (see that file) purely so
/// they render through the same DashboardCard/KPIValue/KpiChart visuals.
/// Its Id uses the same "kpi-*"-style string dispatch KpiIconConverter/
/// KpiChart already use for the real four, so every downstream converter
/// binds the same Id path regardless of which record type supplied it.
/// The actual displayed number is computed at render time
/// (DerivedKpiValueConverter) purely from the real KpiMetricDto values
/// already bound on the page - never a fabricated number.
/// </summary>
public sealed class DerivedKpiSpec
{
    public string Id { get; set; } = string.Empty;
}
