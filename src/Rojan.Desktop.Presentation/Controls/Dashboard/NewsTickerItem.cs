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
}
