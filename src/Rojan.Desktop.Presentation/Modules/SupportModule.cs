using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Support;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>UI Polish Sprint's permanent "پشتیبانی" (Support Center) sidebar entry - visible to every role, same as <see cref="AiCenterModule"/>, so no <see cref="ModuleMetadata.RequiredPermission"/> is set.</summary>
public sealed class SupportModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("support", Strings.Nav_Support, string.Empty, 95);

    public SupportModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<SupportPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
