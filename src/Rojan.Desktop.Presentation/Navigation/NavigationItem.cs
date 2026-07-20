using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Presentation.Modules;

namespace Rojan.Desktop.Presentation.Navigation;

/// <summary>Sidebar display wrapper around a <see cref="ModuleDescriptor"/> - the module system's own metadata, with nothing UI-specific mixed in.</summary>
public sealed record NavigationItem(ModuleDescriptor Descriptor)
{
    public string Title => Descriptor.Metadata.Title;

    public string IconGlyph => Descriptor.Metadata.IconGlyph;

    /// <summary>Phase 22: <see langword="null"/> for every module registered before this phase - see <see cref="ModuleMetadata.RequiredPermission"/>'s own doc comment.</summary>
    public Permission? RequiredPermission => Descriptor.Metadata.RequiredPermission;
}
