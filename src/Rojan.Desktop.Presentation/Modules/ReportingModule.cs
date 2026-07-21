using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Reporting;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>Phase 20's module - replaces the PlaceholderModule that previously registered the "reports" sidebar entry, one-for-one, per the swap documented on <see cref="PlaceholderModule"/> (same pattern <see cref="SettingsModule"/> used in Phase 19A). Phase 33: now the Enterprise Reporting &amp; Business Intelligence "گزارش‌ها" hub - gated by <see cref="Permission.ReportingView"/> at the navigation level (previously unrestricted; the permission existed but was only enforced inside the services themselves).</summary>
public sealed class ReportingModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("reports", Strings.Nav_Reports, string.Empty, 70, Permission.ReportingView);

    public ReportingModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<ReportingPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
