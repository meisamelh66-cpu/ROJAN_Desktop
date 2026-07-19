using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>The one real module today - wraps the existing DI-registered <see cref="DashboardPageViewModel"/> rather than constructing it directly, since it has real dependencies (IDashboardQueryService).</summary>
public sealed class DashboardModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("dashboard", "Dashboard", "", 0);

    public DashboardModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<DashboardPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
