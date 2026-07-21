namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>
/// Phase 35 (Secure Amount Display): the single place that decides which
/// KPI ids are monetary amounts (masked by default, revealed on click) vs.
/// plain counts (customers/bookings/tasks - always visible, per that
/// sprint's explicit "only monetary values should be hidden" requirement).
/// One static list so KPIValue's IsMonetary binding (via
/// KpiIsMonetaryConverter) can't drift from any future consumer.
/// </summary>
internal static class KpiPrivacy
{
    public static bool IsMonetary(string? id) => id is "kpi-revenue" or "derived-avg-transaction";
}
