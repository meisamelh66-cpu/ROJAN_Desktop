namespace Rojan.Desktop.Application.Help;

/// <summary>Phase 26: Help Search. A character range (into the field it was matched against) to render highlighted - Presentation turns this into a bolded/accented <c>Run</c>, this layer only computes where.</summary>
public sealed record HighlightSpan(int Start, int Length);
