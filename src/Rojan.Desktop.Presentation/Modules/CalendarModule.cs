using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.ViewModels.Calendar;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>
/// Phase 14's real module. Unlike Bookings/Specialists/Services, this is
/// not a one-for-one PlaceholderModule swap - no existing placeholder was
/// named "Calendar", so this is a genuinely new sidebar entry, added the
/// same way the Phase 07 module system was designed to support ("adding a
/// module anywhere in the composition root is the only step required for
/// it to appear" - see <see cref="IModuleRegistry"/>). Registering a new
/// <see cref="IModule"/> is composition-root configuration, not a Shell
/// architecture change - nothing about MainWindow, NavigationService, or
/// ModuleRegistry changes to support it.
/// </summary>
public sealed class CalendarModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("calendar", "Calendar", "", 25);

    public CalendarModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<CalendarPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
