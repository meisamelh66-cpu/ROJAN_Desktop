namespace Rojan.Desktop.Domain.Help;

/// <summary>
/// Phase 26: Smart Context Help. One registered help topic - metadata
/// plus a key prefix, never literal display text (Domain/Infrastructure
/// cannot depend on Presentation's <c>Strings</c>, and every field a user
/// reads must be localized per Phase 26.10). <see cref="KeyPrefix"/>
/// (e.g. <c>"Help_Customers"</c>) is expanded into eleven field-specific
/// resx keys at the Presentation boundary
/// (<c>Presentation.Help.HelpContentResolver</c>) by simple suffix
/// concatenation - <c>{KeyPrefix}_Title</c>, <c>{KeyPrefix}_Description</c>,
/// etc. - the same "one shared naming convention instead of one key field
/// per topic" reasoning <c>Strings.GetEnumLabel</c> already established
/// for enum display labels. This keeps a topic's Domain shape small
/// (one string covers eleven content fields) while still requiring zero
/// hardcoded text anywhere below Presentation.
/// </summary>
public sealed record HelpTopic(
    string Id,
    string ModuleId,
    string? PageId,
    string KeyPrefix,
    IReadOnlyList<HelpShortcut> Shortcuts,
    IReadOnlyList<string> RelatedTopicIds,
    string Version);
