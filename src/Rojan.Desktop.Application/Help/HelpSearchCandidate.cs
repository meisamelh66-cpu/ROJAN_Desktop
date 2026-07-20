namespace Rojan.Desktop.Application.Help;

/// <summary>
/// Phase 26: Help Search. One searchable item, supplied by the caller
/// with already-resolved plain text - this layer has no access to
/// <c>Strings</c> (Presentation-only), so it cannot resolve a
/// <see cref="HelpTopicDto.KeyPrefix"/> to display text itself.
/// <c>Presentation.Help.HelpContentResolver</c> resolves every topic's
/// content first, then builds one of these per topic before calling
/// <see cref="IHelpSearchService.Search"/> - keeping the actual
/// scoring/matching algorithm here, reusable and unit-testable, without
/// this layer ever needing to know the text came from localized
/// resources.
/// </summary>
public sealed record HelpSearchCandidate(string TopicId, string Title, string Description, string Overview);
