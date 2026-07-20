namespace Rojan.Desktop.Domain.Help;

/// <summary>
/// Phase 26: Smart Context Help. A keyboard shortcut shown in a help
/// topic. <see cref="KeysDisplay"/> is a literal string (e.g. "Ctrl+N")
/// rather than a localization key - key combination names are the same
/// regardless of the selected language, the same "punctuation/symbols
/// are locale-neutral" reasoning the rest of the app's Run-composition
/// convention already uses. <see cref="DescriptionKey"/> is a
/// localization key (what the shortcut does, which is language-specific
/// text) resolved the same way every other help field is - see
/// <see cref="HelpTopic.KeyPrefix"/>'s own doc comment.
/// </summary>
public sealed record HelpShortcut(string KeysDisplay, string DescriptionKey);
