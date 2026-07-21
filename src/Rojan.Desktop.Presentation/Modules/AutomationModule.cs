using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Automation;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>Phase 32's module - a genuinely new sidebar entry (no "automation" placeholder existed to swap), same as <see cref="AnalyticsModule"/> in Phase 20. Gated behind <see cref="Permission.AutomationView"/> - the first module in this app to actually populate <see cref="ModuleMetadata.RequiredPermission"/> rather than leaving it <see langword="null"/>, now that Requirement 32.13 (Security) needs a real example.</summary>
public sealed class AutomationModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("automation", Strings.Nav_Automation, string.Empty, 85, Permission.AutomationView);

    public AutomationModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<AutomationPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
