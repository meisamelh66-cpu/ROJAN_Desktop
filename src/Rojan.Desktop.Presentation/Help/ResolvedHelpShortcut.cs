namespace Rojan.Desktop.Presentation.Help;

/// <summary>Presentation-only resolved shape of <see cref="Rojan.Desktop.Application.Help.HelpShortcutDto"/> - <see cref="Description"/> is already localized text, never a key.</summary>
public sealed record ResolvedHelpShortcut(string KeysDisplay, string Description);
