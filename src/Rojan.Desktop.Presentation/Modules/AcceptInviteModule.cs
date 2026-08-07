using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Membership;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>
/// Reception Production Integration: a genuinely new sidebar entry, no
/// existing placeholder to swap - same "adding a module anywhere in the
/// composition root is the only step required for it to appear" shape
/// <see cref="SalonModule"/> already established. No <c>RequiredPermission</c>
/// - unconditionally visible, same reasoning <c>SalonModule</c> gives:
/// a user with no real membership yet resolves to the local, all-permission
/// dev/demo role (see <c>CurrentSessionService</c>'s fallback path), so
/// gating this behind a permission would risk locking out the one action
/// that grants every other module's real access. Ordered right after Salon
/// (2, Salon is 1) - the next thing a brand-new staff member (as opposed to
/// a brand-new owner) needs.
/// </summary>
public sealed class AcceptInviteModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("accept-invite", Strings.Nav_AcceptInvite, string.Empty, 2);

    public AcceptInviteModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<AcceptInviteViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
