using Rojan.Desktop.Presentation.Localization;
using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.ViewModels.Inventory;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>Phase 17's real module - replaces the PlaceholderModule that previously registered the "inventory" sidebar entry, one-for-one, per the swap documented on <see cref="PlaceholderModule"/>.</summary>
public sealed class InventoryModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("inventory", Strings.Nav_Inventory, string.Empty, 40);

    public InventoryModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<InventoryPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
