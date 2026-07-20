namespace Rojan.Desktop.Presentation.Help;

/// <summary>Presentation-only display item for a Related Topics/Recently Viewed entry - just enough to render a clickable row without pulling the full <see cref="ResolvedHelpContent"/> until it is actually opened.</summary>
public sealed record RelatedTopicItem(string TopicId, string Title);
