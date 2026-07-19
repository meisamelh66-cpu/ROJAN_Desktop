using Rojan.Desktop.Presentation.Localization;
using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.ViewModels.HR;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>Phase 19's module - a genuinely new sidebar entry (no "hr"/"staff" placeholder existed to swap), same as <see cref="CalendarModule"/> in Phase 15.</summary>
public sealed class HrModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("hr", Strings.Nav_StaffHr, string.Empty, 55);

    public HrModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<HrPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
