using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Salons;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>
/// Phase 1.2 Owner App Create Salon Flow: a genuinely new sidebar entry, no
/// existing placeholder to swap - same "adding a module anywhere in the
/// composition root is the only step required for it to appear" shape
/// <see cref="CalendarModule"/> already established. No <c>RequiredPermission</c>
/// - unconditionally visible, same reasoning
/// <c>Application.Salons.SalonCommandService</c>'s own doc comment gives
/// for why salon creation isn't gated by the local role system either.
/// Ordered right after Dashboard (1, Dashboard is 0) - a brand-new,
/// salon-less owner should find this near the very top, before every other
/// module, all of which assume a salon already exists.
/// </summary>
public sealed class SalonModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("salon", Strings.Nav_Salon, string.Empty, 1);

    public SalonModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<SalonPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
