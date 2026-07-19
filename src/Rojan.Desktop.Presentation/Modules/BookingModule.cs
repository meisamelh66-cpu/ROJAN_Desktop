using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.ViewModels.Bookings;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>Phase 11's real module - replaces the PlaceholderModule that previously registered the "appointments" sidebar entry, one-for-one, per the swap documented on <see cref="PlaceholderModule"/>.</summary>
public sealed class BookingModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("bookings", "Bookings", "◷", 20);

    public BookingModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<BookingPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
