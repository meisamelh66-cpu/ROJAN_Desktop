using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.ViewModels.Services;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>Phase 13's real module - replaces the PlaceholderModule that previously registered the "services" sidebar entry, one-for-one, per the swap documented on <see cref="PlaceholderModule"/>.</summary>
public sealed class ServiceModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("services", "Services", "", 30);

    public ServiceModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<ServicePageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
