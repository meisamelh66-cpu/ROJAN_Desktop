using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.ViewModels.Customers;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>Phase 09's real module - replaces the PlaceholderModule that previously registered the "customers" sidebar entry, one-for-one, per the swap documented on <see cref="PlaceholderModule"/>.</summary>
public sealed class CustomerModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("customers", "Customers", "◈", 10);

    public CustomerModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<CustomerPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
