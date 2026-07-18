namespace Rojan.Desktop.Presentation.Modules;

/// <summary>
/// Pure display data for a module - everything the sidebar needs to show
/// an entry, with no knowledge of how that module actually activates.
/// Kept separate from <see cref="ModuleDescriptor"/> so navigation UI can
/// depend on metadata alone without pulling in activation concerns.
/// </summary>
public sealed record ModuleMetadata(string Id, string Title, string IconGlyph, int Order);
