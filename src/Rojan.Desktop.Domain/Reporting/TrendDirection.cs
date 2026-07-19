namespace Rojan.Desktop.Domain.Reporting;

/// <summary>
/// Direction of change for a <see cref="KpiValue"/> since its prior
/// comparison period - a Reporting-owned duplicate of
/// <c>Domain.Dashboard.TrendDirection</c>, not a cross-module reference,
/// same "Domain modules don't reference each other" isolation every other
/// slice already follows (see <see cref="ReportSnapshot"/>'s doc comment
/// for the same reasoning applied to identifiers).
/// </summary>
public enum TrendDirection
{
    Flat,
    Up,
    Down,
}
