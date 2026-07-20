namespace Rojan.Desktop.Application.Help;

/// <summary>Application-layer mirror of <see cref="Rojan.Desktop.Domain.Help.HelpShortcut"/> - see <see cref="HelpTopicDto"/>'s own doc comment for why the mapping exists.</summary>
public sealed record HelpShortcutDto(string KeysDisplay, string DescriptionKey);
