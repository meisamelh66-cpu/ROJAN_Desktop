namespace Rojan.Desktop.Presentation.Help;

/// <summary>
/// Phase 26: Smart Context Help. A help topic with every field already
/// resolved to display text - the shape <c>ViewModels.Help.HelpDialogViewModel</c>
/// and <c>Views.Help.ContextHelpDialogView</c> actually bind to.
/// <see cref="IHelpContentResolver"/> is the only place a
/// <see cref="Rojan.Desktop.Application.Help.HelpTopicDto"/> is turned
/// into one of these, since only Presentation can see <c>Strings</c>.
/// </summary>
public sealed record ResolvedHelpContent(
    string TopicId,
    string ModuleId,
    string? PageId,
    string Title,
    string Subtitle,
    string Description,
    string Purpose,
    string Overview,
    string WhenToUse,
    IReadOnlyList<string> Steps,
    IReadOnlyList<string> Tips,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> BestPractices,
    IReadOnlyList<string> Notes,
    IReadOnlyList<ResolvedHelpShortcut> Shortcuts,
    IReadOnlyList<string> RelatedTopicIds);
