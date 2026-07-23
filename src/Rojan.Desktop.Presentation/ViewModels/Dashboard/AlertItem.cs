namespace Rojan.Desktop.Presentation.ViewModels.Dashboard;

/// <summary>Phase C-2 (Recent Alerts Panel): a plain presentation-only model, same reasoning as <see cref="StaffStatusItem"/>.</summary>
public sealed class AlertItem
{
    /// <summary>An icon resource key (e.g. "Rojan.Icon.Warning"), resolved to a glyph by RecentAlertsPanel's ResourceKeyToGlyphConverter - keeps this a plain string, no WPF resource lookup inside the model/ViewModel.</summary>
    public required string Icon { get; init; }

    public required string Text { get; init; }
}
