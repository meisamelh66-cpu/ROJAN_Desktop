using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Analytics;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>Phase 20's second module - a genuinely new sidebar entry (no "analytics" placeholder existed to swap), same as <see cref="CalendarModule"/> in Phase 15 and <see cref="HrModule"/> in Phase 19.</summary>
public sealed class AnalyticsModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("analytics", Strings.Nav_Analytics, string.Empty, 75);

    public AnalyticsModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<AnalyticsPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
