namespace Rojan.Desktop.Domain.Search;

/// <summary>Phase 28: Enterprise Global Search &amp; Command Palette. One matched character run within a searched text field - the Search Highlighting requirement's raw data, before Presentation renders it.</summary>
public sealed record MatchSpan(int Start, int Length);
