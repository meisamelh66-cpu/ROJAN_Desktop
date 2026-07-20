using Rojan.Desktop.Application.Help;
using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Presentation.Help;

/// <summary>
/// Default <see cref="IHelpContentResolver"/>. Expands
/// <see cref="HelpTopicDto.KeyPrefix"/> into the eleven field-specific
/// resx keys via <see cref="Strings.GetByKey"/> - see
/// <see cref="Rojan.Desktop.Domain.Help.HelpTopic.KeyPrefix"/>'s own doc
/// comment for the naming convention. List-shaped fields (Steps/Tips/
/// Warnings/BestPractices/Notes) are stored as one newline-separated resx
/// value each rather than one key per list item, split here into
/// individual entries.
/// </summary>
public sealed class HelpContentResolver : IHelpContentResolver
{
    private static readonly string[] NewLineSeparators = ["\r\n", "\n"];

    public ResolvedHelpContent Resolve(HelpTopicDto topic)
    {
        var prefix = topic.KeyPrefix;

        return new ResolvedHelpContent(
            TopicId: topic.Id,
            ModuleId: topic.ModuleId,
            PageId: topic.PageId,
            Title: Strings.GetByKey($"{prefix}_Title"),
            Subtitle: Strings.GetByKey($"{prefix}_Subtitle"),
            Description: Strings.GetByKey($"{prefix}_Description"),
            Purpose: Strings.GetByKey($"{prefix}_Purpose"),
            Overview: Strings.GetByKey($"{prefix}_Overview"),
            WhenToUse: Strings.GetByKey($"{prefix}_WhenToUse"),
            Steps: SplitLines(Strings.GetByKey($"{prefix}_Steps")),
            Tips: SplitLines(Strings.GetByKey($"{prefix}_Tips")),
            Warnings: SplitLines(Strings.GetByKey($"{prefix}_Warnings")),
            BestPractices: SplitLines(Strings.GetByKey($"{prefix}_BestPractices")),
            Notes: SplitLines(Strings.GetByKey($"{prefix}_Notes")),
            Shortcuts: topic.Shortcuts.Select(s => new ResolvedHelpShortcut(s.KeysDisplay, Strings.GetByKey(s.DescriptionKey))).ToList(),
            RelatedTopicIds: topic.RelatedTopicIds);
    }

    private static string[] SplitLines(string value) =>
        value.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
