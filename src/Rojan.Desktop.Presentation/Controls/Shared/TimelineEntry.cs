namespace Rojan.Desktop.Presentation.Controls.Shared;

/// <summary>
/// One row in a Timeline - an icon glyph key (see Themes/Icons.xaml), a
/// title, and optional description/already-formatted timestamp text (the
/// caller is responsible for culture-aware date formatting, the same
/// "already-formatted string in, control never touches CultureInfo" shape
/// KPIValue.Value already uses). Presentation-only; never a ViewModel/DTO,
/// same convention as Controls/Dashboard/NewsTickerItem.cs.
/// </summary>
public sealed class TimelineEntry
{
    public required string Icon { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public string? TimestampText { get; init; }
}
