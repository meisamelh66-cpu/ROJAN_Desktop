namespace Rojan.Desktop.Application.Notifications;

/// <summary>One matched substring's position within a resolved text field - the Notification Search requirement's "highlight matches", same shape as <c>Application.Help.HighlightSpan</c>.</summary>
public sealed record HighlightSpan(int Start, int Length);
