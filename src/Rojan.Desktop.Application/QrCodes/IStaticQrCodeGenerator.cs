namespace Rojan.Desktop.Application.QrCodes;

/// <summary>
/// QR Ecosystem (Desktop Productionization Sprint 1): generates a
/// scannable PNG for a URL that has nothing to do with a specific salon
/// or invite - today, only the "Manager QR" (a static link to the ROJAN
/// Manager app's download page). Everything salon/invite-specific goes
/// through the real backend instead (<c>Salons.ISalonQueryService.GetSalonQrCodeAsync</c>/
/// <c>Membership.ISalonInviteService.GetInviteQrCodeAsync</c>) so the URL
/// itself is never guessed or duplicated client-side - this generator
/// exists only for the one case that has no backend resource to ask.
///
/// Lives in Application, not Domain: unlike a repository abstraction,
/// this has no salon/invite domain concept behind it at all - it is
/// exactly the same shape as <c>Api.IApiClient</c> (an Application-defined
/// port whose concrete implementation is an Infrastructure-layer
/// technology choice, here "which QR library" instead of "which HTTP
/// client"). Presentation depends on this interface directly
/// (<c>ViewModels.QrCodes.QrCodesPageViewModel</c>) - it must not live in
/// Infrastructure, or that dependency would violate the same Presentation-
/// never-references-Infrastructure boundary <c>ArchitectureTests.DependencyDirectionTests</c>
/// already enforces (see the Reception Production Integration phase's own
/// "mid-phase correction" for the identical mistake this avoids).
/// </summary>
public interface IStaticQrCodeGenerator
{
    public byte[] GeneratePng(string url, int sizePx);
}
