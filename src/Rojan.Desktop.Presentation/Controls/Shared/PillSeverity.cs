namespace Rojan.Desktop.Presentation.Controls.Shared;

/// <summary>StatusPill's color role. Info reuses the app's one interactive Accent color (no dedicated "info" brush exists in the design system); Neutral uses MutedText.</summary>
public enum PillSeverity
{
    Neutral,
    Info,
    Success,
    Warning,
    Error,
}
