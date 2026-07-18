using Rojan.Desktop.Presentation.Modules;

namespace Rojan.Desktop.Presentation.Navigation;

/// <summary>Sidebar display wrapper around a <see cref="ModuleDescriptor"/> - the module system's own metadata, with nothing UI-specific mixed in.</summary>
public sealed record NavigationItem(ModuleDescriptor Descriptor)
{
    public string Title => Descriptor.Metadata.Title;

    public string IconGlyph => Descriptor.Metadata.IconGlyph;
}
