namespace Rojan.Desktop.Application.Dashboard;

/// <summary>
/// Application's own copy of the trend-direction concept, distinct from
/// <see cref="Rojan.Desktop.Domain.Dashboard.TrendDirection"/>. Presentation
/// references Application only, never Domain directly - so anything
/// Presentation binds to must be shaped by an Application-owned type, not a
/// Domain one. <see cref="Dashboard.DashboardQueryService"/> maps between
/// the two explicitly.
/// </summary>
public enum TrendDirection
{
    Flat,
    Up,
    Down,
}
