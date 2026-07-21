using System.Globalization;

namespace Rojan.Desktop.Domain.Search;

/// <summary>
/// Phase 28: Enterprise Global Search &amp; Command Palette. Pure text-
/// matching logic - no I/O, no localization awareness, matches the
/// "value/data in, decision out" shape every other <c>*Rules</c> class in
/// this codebase already uses (e.g. <see cref="Help.HelpContentRules"/>,
/// <see cref="Notifications.NotificationRules"/>). Tries, in order:
/// exact match, prefix match, substring match (all three culture-aware
/// via <see cref="CompareInfo"/>, correct for Persian/Arabic text) and
/// only falls back to fuzzy subsequence matching (every query character
/// appears in <c>text</c> in order, not necessarily contiguous - the
/// "Fuzzy matching" requirement) when none of those found anything.
/// Each tier scores strictly lower
/// than the one before it, so an exact/substring hit always outranks a
/// fuzzy one regardless of query length - the "Intelligent ranking"
/// requirement's foundation.
/// </summary>
public static class SearchRules
{
    private const double ExactMatchScore = 100;
    private const double PrefixMatchScore = 80;
    private const double SubstringMatchScore = 60;
    private const double FuzzyBaseScore = 20;

    public static FuzzyMatchResult Match(string query, string text)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrEmpty(text))
        {
            return FuzzyMatchResult.NoMatch;
        }

        var trimmedQuery = query.Trim();
        var compareInfo = CultureInfo.CurrentCulture.CompareInfo;

        if (compareInfo.Compare(text, trimmedQuery, CompareOptions.IgnoreCase) == 0)
        {
            return new FuzzyMatchResult(true, ExactMatchScore, [new MatchSpan(0, text.Length)]);
        }

        var substringIndex = compareInfo.IndexOf(text, trimmedQuery, CompareOptions.IgnoreCase);
        if (substringIndex >= 0)
        {
            var score = substringIndex == 0 ? PrefixMatchScore : SubstringMatchScore;
            return new FuzzyMatchResult(true, score, [new MatchSpan(substringIndex, trimmedQuery.Length)]);
        }

        return MatchFuzzySubsequence(trimmedQuery, text);
    }

    /// <summary>
    /// Every character of <paramref name="query"/> must appear in
    /// <paramref name="text"/>, in order, not necessarily contiguous
    /// (e.g. query <c>"cst"</c> matches text <c>"Customers"</c>) - a
    /// simple case-folded per-character comparison rather than
    /// <see cref="CompareInfo"/> (which operates on whole strings, not
    /// efficiently per character); Persian/Arabic letters have no case
    /// distinction, so this stays correct for them too. Rewards
    /// contiguous runs and an early first match so a tighter, earlier
    /// fuzzy match still outranks a looser, later one.
    /// </summary>
    private static FuzzyMatchResult MatchFuzzySubsequence(string query, string text)
    {
        var spans = new List<MatchSpan>();
        var textPosition = 0;
        var score = FuzzyBaseScore;
        var consecutiveRun = 0;

        foreach (var queryChar in query)
        {
            var foundAt = -1;
            for (var i = textPosition; i < text.Length; i++)
            {
                if (char.ToUpperInvariant(text[i]) == char.ToUpperInvariant(queryChar))
                {
                    foundAt = i;
                    break;
                }
            }

            if (foundAt < 0)
            {
                return FuzzyMatchResult.NoMatch;
            }

            if (spans.Count > 0 && spans[^1].Start + spans[^1].Length == foundAt)
            {
                spans[^1] = spans[^1] with { Length = spans[^1].Length + 1 };
                consecutiveRun++;
                score += 2 + consecutiveRun;
            }
            else
            {
                spans.Add(new MatchSpan(foundAt, 1));
                consecutiveRun = 0;
                score += 1;
            }

            if (foundAt == 0)
            {
                score += 5;
            }

            textPosition = foundAt + 1;
        }

        return new FuzzyMatchResult(true, score, spans);
    }
}
