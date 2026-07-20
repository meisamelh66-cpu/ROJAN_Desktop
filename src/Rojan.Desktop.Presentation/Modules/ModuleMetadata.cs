using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>
/// Pure display data for a module - everything the sidebar needs to show
/// an entry, with no knowledge of how that module actually activates.
/// Kept separate from <see cref="ModuleDescriptor"/> so navigation UI can
/// depend on metadata alone without pulling in activation concerns.
///
/// Phase 22: <see cref="RequiredPermission"/> is an additive, optional
/// trailing parameter (defaults to <see langword="null"/>) - every module
/// registered before this phase keeps constructing
/// <c>ModuleMetadata(id, title, icon, order)</c> unchanged and stays
/// unconditionally visible (no permission required); only a module that
/// explicitly opts in by passing one gets filtered by
/// <c>MainWindowViewModel</c>'s permission-aware navigation. This is the
/// entire mechanism - no existing module's registration needed to change.
/// </summary>
public sealed record ModuleMetadata(string Id, string Title, string IconGlyph, int Order, Permission? RequiredPermission = null);
