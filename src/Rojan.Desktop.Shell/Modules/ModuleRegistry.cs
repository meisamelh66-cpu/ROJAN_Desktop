using Rojan.Desktop.Presentation.Modules;

namespace Rojan.Desktop.Shell.Modules;

/// <summary>
/// Concrete <see cref="IModuleRegistry"/> - aggregates every <see cref="IModule"/>
/// registered in DI (via <c>IEnumerable&lt;IModule&gt;</c> injection, the
/// standard multi-registration pattern) and exposes their descriptors
/// ordered for display. Adding a module anywhere in the composition root
/// is the only step required for it to appear here - this class has no
/// knowledge of what modules exist.
/// </summary>
public sealed class ModuleRegistry : IModuleRegistry
{
    public ModuleRegistry(IEnumerable<IModule> modules)
    {
        Modules = modules
            .Select(module => module.Descriptor)
            .OrderBy(descriptor => descriptor.Metadata.Order)
            .ToList();
    }

    public IReadOnlyList<ModuleDescriptor> Modules { get; }
}
