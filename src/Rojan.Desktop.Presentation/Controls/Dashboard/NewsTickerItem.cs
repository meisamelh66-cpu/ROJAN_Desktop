namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>
/// Phase 36 (Live News Ticker): one scrolling ticker entry - a Fluent icon
/// glyph key (see Themes/Icons.xaml), the display text, and an optional
/// click action (typically navigation via DashboardNavigationBridge, or a
/// "coming soon" message for categories with no real destination page -
/// same pattern as the Quick Actions bug fix). Presentation-only; never a
/// ViewModel/DTO.
/// </summary>
public sealed class NewsTickerItem
{
    public required string Icon { get; init; }

    public required string Text { get; init; }

    public Action? OnClick { get; init; }

    /// <summary>Phase B-1 (visual refinement): shows a small "LIVE" badge before the item - reference-parity for the one ticker item (a VIP birthday alert) that reads as a live/urgent notice, not a generic category. Additive/optional - every existing 3-field item initializer still compiles unchanged.</summary>
    public bool IsLive { get; init; }
}
