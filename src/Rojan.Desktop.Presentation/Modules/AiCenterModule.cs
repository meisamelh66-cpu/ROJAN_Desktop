using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.AI;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>Phase 21's module - replaces the PlaceholderModule that previously registered the "ai-center" sidebar entry, one-for-one, per the swap documented on <see cref="PlaceholderModule"/> (same pattern <see cref="ReportingModule"/> used in Phase 20).</summary>
public sealed class AiCenterModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("ai-center", Strings.Nav_AiCenter, string.Empty, 80);

    public AiCenterModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<AiCenterPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
