using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Settings;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>Phase 19A's real module - replaces the PlaceholderModule that previously registered the "settings" sidebar entry, one-for-one, per the swap documented on <see cref="PlaceholderModule"/>.</summary>
public sealed class SettingsModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("settings", Strings.Nav_Settings, string.Empty, 90);

    public SettingsModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<SettingsPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
