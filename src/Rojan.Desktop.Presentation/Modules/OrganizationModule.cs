using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Organizations;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>Phase 22's module - the Organization &amp; Branches admin surface, gated by <see cref="Permission.OrganizationManage"/> so it only appears for workspace roles the Permission Engine actually grants it to (see <c>RolePermissions</c>).</summary>
public sealed class OrganizationModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("organizations", Strings.Nav_Organizations, string.Empty, 5, Permission.OrganizationManage);

    public OrganizationModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<OrganizationPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
