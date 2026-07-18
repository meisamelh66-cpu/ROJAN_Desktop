namespace Rojan.Desktop.Presentation.Modules;

/// <summary>
/// A pluggable top-level section of the application. Every future module
/// (CRM, Booking, Inventory, ...) implements this and registers itself as
/// an <c>IModule</c> in the composition root - the shell discovers it
/// through <see cref="IModuleRegistry"/> without ever referencing the
/// module's own types, and the module never references another module.
/// </summary>
public interface IModule
{
    public ModuleDescriptor Descriptor { get; }
}
