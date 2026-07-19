using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.ViewModels.Accounting;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>Phase 18's real module - replaces the PlaceholderModule that previously registered the "accounting" sidebar entry, one-for-one, per the swap documented on <see cref="PlaceholderModule"/>.</summary>
public sealed class AccountingModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("accounting", "Accounting", "", 50);

    public AccountingModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<AccountingPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
