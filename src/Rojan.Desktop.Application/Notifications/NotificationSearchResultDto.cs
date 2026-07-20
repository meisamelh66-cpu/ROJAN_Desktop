namespace Rojan.Desktop.Application.Notifications;

/// <summary>One ranked <see cref="NotificationSearchService"/> match - the matched notification's id plus per-field highlight spans Presentation renders via its <c>HighlightText</c> attached property (shared with Phase 26's Help Search).</summary>
public sealed record NotificationSearchResultDto(
    string NotificationId,
    double Score,
    IReadOnlyList<HighlightSpan> TitleHighlights,
    IReadOnlyList<HighlightSpan> MessageHighlights);
