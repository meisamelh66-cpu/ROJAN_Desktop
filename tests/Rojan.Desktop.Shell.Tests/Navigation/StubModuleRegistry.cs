using Rojan.Desktop.Presentation.Modules;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>Fixed-list <see cref="IModuleRegistry"/> test double - lets a test control exactly which modules (and their <see cref="ModuleMetadata.RequiredPermission"/>) exist.</summary>
internal sealed class StubModuleRegistry : IModuleRegistry
{
    public StubModuleRegistry(IReadOnlyList<ModuleDescriptor> modules)
    {
        Modules = modules;
    }

    public IReadOnlyList<ModuleDescriptor> Modules { get; }
}
