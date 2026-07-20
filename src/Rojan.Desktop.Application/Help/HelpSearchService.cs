using System.Globalization;

namespace Rojan.Desktop.Application.Help;

/// <summary>
/// Default <see cref="IHelpSearchService"/>. Case-insensitive, culture-
/// aware substring matching (<see cref="CompareOptions.IgnoreCase"/> via
/// <see cref="CompareInfo.IndexOf(string, string, CompareOptions)"/> -
/// correct for Persian/Arabic text, unlike a naive
/// <see cref="string.ToUpperInvariant"/> comparison) against
/// <see cref="HelpSearchCandidate.Title"/> (weight 3),
/// <see cref="HelpSearchCandidate.Description"/> (weight 1.5), and
/// <see cref="HelpSearchCandidate.Overview"/> (weight 1) - a title match
/// ranks a topic far above one that only happens to mention the query
/// deep in its overview. Every occurrence within a field adds to that
/// field's score (so "customer" appearing twice in a description scores
/// higher than once), and every occurrence is highlighted, not just the
/// first.
/// </summary>
public sealed class HelpSearchService : IHelpSearchService
{
    private const double TitleWeight = 3;
    private const double DescriptionWeight = 1.5;
    private const double OverviewWeight = 1;
    private const int SnippetLength = 140;

    public IReadOnlyList<HelpSearchResultDto> Search(IReadOnlyList<HelpSearchCandidate> candidates, string query)
    {
        var trimmedQuery = query.Trim();
        if (trimmedQuery.Length == 0)
        {
            return [];
        }

        var results = new List<HelpSearchResultDto>();
        foreach (var candidate in candidates)
        {
            var titleHits = FindOccurrences(candidate.Title, trimmedQuery);
            var descriptionHits = FindOccurrences(candidate.Description, trimmedQuery);
            var overviewHits = FindOccurrences(candidate.Overview, trimmedQuery);

            var score = (titleHits.Count * TitleWeight) + (descriptionHits.Count * DescriptionWeight) + (overviewHits.Count * OverviewWeight);
            if (score <= 0)
            {
                continue;
            }

            var (snippetSource, snippetHitsInSource) = descriptionHits.Count > 0
                ? (candidate.Description, descriptionHits)
                : overviewHits.Count > 0
                    ? (candidate.Overview, overviewHits)
                    : (candidate.Title, titleHits);

            var (snippet, snippetHighlights) = BuildSnippet(snippetSource, snippetHitsInSource);

            results.Add(new HelpSearchResultDto(candidate.TopicId, candidate.Title, snippet, score, titleHits, snippetHighlights));
        }

        return results
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static List<HighlightSpan> FindOccurrences(string text, string query)
    {
        var spans = new List<HighlightSpan>();
        if (text.Length == 0)
        {
            return spans;
        }

        var compareInfo = CultureInfo.CurrentCulture.CompareInfo;
        var searchStart = 0;
        while (searchStart <= text.Length - query.Length)
        {
            var index = compareInfo.IndexOf(text, query, searchStart, CompareOptions.IgnoreCase);
            if (index < 0)
            {
                break;
            }

            spans.Add(new HighlightSpan(index, query.Length));
            searchStart = index + query.Length;
        }

        return spans;
    }

    private static (string Snippet, IReadOnlyList<HighlightSpan> Highlights) BuildSnippet(string source, List<HighlightSpan> hitsInSource)
    {
        if (source.Length <= SnippetLength || hitsInSource.Count == 0)
        {
            return (source, hitsInSource);
        }

        var firstHit = hitsInSource[0];
        var windowStart = Math.Max(0, firstHit.Start - (SnippetLength / 3));
        var windowLength = Math.Min(SnippetLength, source.Length - windowStart);
        var snippet = source.Substring(windowStart, windowLength);

        var adjustedHighlights = hitsInSource
            .Where(hit => hit.Start >= windowStart && hit.Start + hit.Length <= windowStart + windowLength)
            .Select(hit => hit with { Start = hit.Start - windowStart })
            .ToList();

        return (snippet, adjustedHighlights);
    }
}
