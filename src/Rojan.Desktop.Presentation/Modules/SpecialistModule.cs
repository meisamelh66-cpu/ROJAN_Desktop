using Rojan.Desktop.Presentation.Localization;
using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.ViewModels.Specialists;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>Phase 12's real module - replaces the PlaceholderModule that previously registered the "employees" sidebar entry, one-for-one, per the swap documented on <see cref="PlaceholderModule"/>.</summary>
public sealed class SpecialistModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("specialists", Strings.Nav_Specialists, string.Empty, 60);

    public SpecialistModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<SpecialistPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
