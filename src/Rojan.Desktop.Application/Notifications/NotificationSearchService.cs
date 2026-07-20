using System.Globalization;

namespace Rojan.Desktop.Application.Notifications;

/// <summary>
/// Default <see cref="INotificationSearchService"/>. Case-insensitive,
/// culture-aware substring matching (<see cref="CompareOptions.IgnoreCase"/>
/// via <see cref="CompareInfo.IndexOf(string, string, CompareOptions)"/> -
/// correct for Persian/Arabic text) against
/// <see cref="NotificationSearchCandidate.Title"/> (weight 2) and
/// <see cref="NotificationSearchCandidate.Message"/> (weight 1) - a
/// title match ranks a notification above one that only happens to
/// mention the query in its body. Every occurrence within a field adds
/// to that field's score and is highlighted, not just the first -
/// mirrors <see cref="Help.HelpSearchService"/>'s algorithm shape,
/// scaled down for a notification's two (not five) text fields.
/// </summary>
public sealed class NotificationSearchService : INotificationSearchService
{
    private const double TitleWeight = 2;
    private const double MessageWeight = 1;

    public IReadOnlyList<NotificationSearchResultDto> Search(IReadOnlyList<NotificationSearchCandidate> candidates, string query)
    {
        var trimmedQuery = query.Trim();
        if (trimmedQuery.Length == 0)
        {
            return candidates
                .Select(c => new NotificationSearchResultDto(c.NotificationId, 0, [], []))
                .ToList();
        }

        var results = new List<NotificationSearchResultDto>();
        foreach (var candidate in candidates)
        {
            var titleHits = FindOccurrences(candidate.Title, trimmedQuery);
            var messageHits = FindOccurrences(candidate.Message, trimmedQuery);

            var score = (titleHits.Count * TitleWeight) + (messageHits.Count * MessageWeight);
            if (score <= 0)
            {
                continue;
            }

            results.Add(new NotificationSearchResultDto(candidate.NotificationId, score, titleHits, messageHits));
        }

        return results
            .OrderByDescending(result => result.Score)
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
}
