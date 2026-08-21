using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.QrCodes;

namespace Rojan.Desktop.Presentation.Modules;

/// <summary>
/// QR Ecosystem (Desktop Productionization Sprint 1): a genuinely new
/// sidebar entry, no existing placeholder to swap - same "adding a module
/// anywhere in the composition root is the only step required for it to
/// appear" shape <see cref="SalonModule"/> already established. Gated by
/// <see cref="Permission.ManageUsers"/> - the existing permission granted
/// to Owner/Manager/BranchManager but not Reception/Specialist
/// (<c>Domain.Organizations.RolePermissions</c>), matching the real
/// backend's own owner-only <c>MANAGE_SALON</c>/<c>MANAGE_MEMBERSHIP</c>
/// gating on the two endpoints this page calls
/// (<c>GET /salons/{id}/qr-code</c>, <c>POST /salons/{id}/invites</c>) -
/// unlike <see cref="SalonModule"/>/<see cref="AcceptInviteModule"/>, this
/// one has a real permission to gate on since it never needs to be reached
/// by a brand-new, not-yet-onboarded session. Ordered right after Salon
/// (3, Salon is 1, AcceptInvite is 2) - printable onboarding material is
/// naturally the next thing an owner wants once their salon exists.
/// </summary>
public sealed class QrCodesModule : IModule
{
    private static readonly ModuleMetadata Metadata = new("qr-codes", Strings.Nav_QrCodes, string.Empty, 3, Permission.ManageUsers);

    public QrCodesModule()
    {
        Descriptor = new ModuleDescriptor(Metadata, sp => sp.GetRequiredService<QrCodesPageViewModel>());
    }

    public ModuleDescriptor Descriptor { get; }
}
