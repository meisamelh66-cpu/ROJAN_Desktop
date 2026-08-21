namespace Rojan.Desktop.Application.Membership;

/// <summary>Read/accept use case Presentation depends on for the invite-accept flow - the only way Presentation ever reaches it, never through Domain/Infrastructure directly, same convention <c>Salons.ISalonQueryService</c> already establishes.</summary>
public interface ISalonInviteService
{
    public Task<SalonInviteDetailsDto> GetDetailsAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Accepts the invite *and* persists the resulting membership locally (via <see cref="IAcceptedMembershipStore"/>) as one operation from the caller's perspective - Presentation never touches that Domain-typed store directly, only this Application-layer facade.</summary>
    public Task<AcceptedMembershipDto> AcceptAsync(string token, string salonName, CancellationToken cancellationToken = default);

    /// <summary>
    /// QR Ecosystem (Desktop Productionization Sprint 1): creates a new
    /// <c>Receptionist</c>-role invite for <paramref name="salonId"/> -
    /// the owner-side counterpart to <see cref="GetDetailsAsync"/>/
    /// <see cref="AcceptAsync"/>'s invitee-side flow. Fixed to
    /// <c>SalonRole.Receptionist</c> rather than taking a role parameter:
    /// this is the one invite type <c>ViewModels.QrCodes.QrCodesPageViewModel</c>
    /// needs (a printable staff-onboarding QR at reception) - a
    /// general-purpose "invite any role" flow is a documented, not silent,
    /// out-of-scope boundary for this phase (see this repository interface's
    /// own <c>SalonRole.Manager</c> doc comment).
    /// </summary>
    public Task<CreatedInviteDto> CreateReceptionInviteAsync(string salonId, CancellationToken cancellationToken = default);

    /// <summary>QR Ecosystem: the invite's scannable accept-link PNG, ready to display/print.</summary>
    public Task<byte[]> GetInviteQrCodeAsync(string salonId, string inviteId, int sizePx, CancellationToken cancellationToken = default);
}
