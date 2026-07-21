namespace Rojan.Desktop.Application.Search;

/// <summary>One matched substring's position within a result's title - the Search Highlighting requirement's display shape, mapped from <see cref="Domain.Search.MatchSpan"/> by <see cref="SearchRankingService"/> so Presentation never needs a Domain reference (same "Application owns its own mirror type" shape <c>Application.Notifications.HighlightSpan</c> already established).</summary>
public sealed record HighlightSpan(int Start, int Length);
