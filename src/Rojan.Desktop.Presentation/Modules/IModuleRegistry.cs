namespace Rojan.Desktop.Presentation.Modules;

/// <summary>Aggregates every registered <see cref="IModule"/> - the sidebar's only source of what sections exist.</summary>
public interface IModuleRegistry
{
    /// <summary>Every registered module's descriptor, ordered for display.</summary>
    public IReadOnlyList<ModuleDescriptor> Modules { get; }
}
