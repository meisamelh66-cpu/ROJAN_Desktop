using Rojan.Desktop.Presentation.Modules;

namespace Rojan.Desktop.Presentation.Tests.Workspaces;

/// <summary>Fixed-list <see cref="IModuleRegistry"/> test double.</summary>
internal sealed class StubModuleRegistry : IModuleRegistry
{
    public StubModuleRegistry(IReadOnlyList<ModuleDescriptor> modules)
    {
        Modules = modules;
    }

    public IReadOnlyList<ModuleDescriptor> Modules { get; }
}
