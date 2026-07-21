namespace Rojan.Desktop.Domain.Search;

/// <summary>Phase 28: Enterprise Global Search &amp; Command Palette. The outcome of <see cref="SearchRules.Match"/> against one text field - whether the query matched at all, how well (<see cref="Score"/>, higher is better), and where (<see cref="Spans"/>, for highlighting).</summary>
public sealed record FuzzyMatchResult(bool IsMatch, double Score, IReadOnlyList<MatchSpan> Spans)
{
    public static readonly FuzzyMatchResult NoMatch = new(false, 0, []);
}
