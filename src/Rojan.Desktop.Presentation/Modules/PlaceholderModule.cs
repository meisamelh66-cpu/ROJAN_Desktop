using Rojan.Desktop.Presentation.ViewModels.Modules;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>
/// One reusable <see cref="IModule"/> for every not-yet-implemented
/// section - Phase 07 is architecture-first, so Customers, Appointments,
/// Services, Inventory, Accounting, Employees, Reports, AI Center, and
/// Settings all register an instance of this (with their own metadata)
/// rather than each needing a bespoke module class/View. A real module
/// implementation replaces its instance of this one-for-one when that
/// section is actually built - registration is the only change required
/// elsewhere.
/// </summary>
public sealed class PlaceholderModule : IModule
{
    public PlaceholderModule(ModuleMetadata metadata)
    {
        Descriptor = new ModuleDescriptor(metadata, _ => new PlaceholderModuleViewModel(metadata.Title));
    }

    public ModuleDescriptor Descriptor { get; }
}
