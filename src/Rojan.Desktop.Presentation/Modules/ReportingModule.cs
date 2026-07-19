using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Reporting;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>Phase 20's module - replaces the PlaceholderModule that previously registered the "reports" sidebar entry, one-for-one, per the swap documented on <see cref="PlaceholderModule"/> (same pattern <see cref="SettingsModule"/> used in Phase 19A).</summary>
public sealed class ReportingModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("reports", Strings.Nav_Reports, string.Empty, 70);

    public ReportingModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<ReportingPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
