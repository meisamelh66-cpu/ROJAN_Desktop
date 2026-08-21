namespace Rojan.Desktop.Domain.Membership;

/// <summary>
/// QR Ecosystem (Desktop Productionization Sprint 1): the role a new
/// staff invite grants - mirrors ROJAN_Backend's own <c>SalonRole</c>
/// enum (<c>domain/salon/SalonRole.kt</c>: only <c>MANAGER</c>/
/// <c>RECEPTIONIST</c> exist there, no <c>SPECIALIST</c> - confirmed by
/// direct inspection before this feature was built, since the original
/// brief's "Specialist QR" had no corresponding backend role or product
/// to encode). <see cref="Manager"/> is defined for completeness/parity
/// with the backend contract but not exposed by this phase's QR page -
/// only <see cref="Receptionist"/> is (the repurposed "staff-invite QR",
/// printed at reception for a new front-desk hire to scan and join).
/// </summary>
public enum SalonRole
{
    Manager,
    Receptionist,
}

/// <summary>QR Ecosystem: the result of creating a new staff invite - just enough for the immediate next step (<see cref="ISalonInviteRepository.GetInviteQrCodeAsync"/>), not the full <c>SalonInviteResponse</c> shape ROJAN_Backend returns (status/expiry/etc. belong to the not-yet-built invite-list UI, a documented out-of-scope gap for this phase).</summary>
public sealed record CreatedSalonInvite(string InviteId, string Token);

/// <summary>
/// Reception Production Integration: what an invite token resolves to
/// before it's accepted - the confirmation-screen shape ("join {SalonName}
/// as {Role}?"), matching ROJAN_Backend's own deliberately minimal
/// <c>GET /api/v1/invites/{token}</c> response (never leaks anything about
/// the invite beyond salon name and role).
/// </summary>
public sealed record SalonInviteDetails(string SalonName, string Role);

/// <summary>
/// The result of accepting an invite - both the return value of
/// <see cref="ISalonInviteRepository.AcceptAsync"/> and, once persisted via
/// <c>Application.Membership.IAcceptedMembershipStore</c>, the record this
/// app treats as "who am I, and at which salon" for a non-owner session.
/// <see cref="SalonName"/> is not part of ROJAN_Backend's accept response -
/// carried over from the preceding <see cref="SalonInviteDetails"/> lookup
/// so the persisted record is self-describing without a second round trip.
/// </summary>
public sealed record AcceptedMembership(string SalonId, string SalonName, string Role);

/// <summary>
/// Repository abstraction for the Salon Invite accept flow. Domain defines
/// the contract; Infrastructure provides the concrete implementation - same
/// split as <see cref="Rojan.Desktop.Domain.Salons.ISalonRepository"/>.
/// Deliberately token-scoped, not salon-scoped: unlike every other
/// <c>Backend*Repository</c> in this app, the caller does not yet have any
/// salon membership when using this one - the token itself is the only
/// thing identifying which salon/role is in play, matching
/// <c>ISalonRepository</c>'s own "this repository *is* how a caller
/// discovers X" reasoning.
/// </summary>
public interface ISalonInviteRepository
{
    public Task<SalonInviteDetails> GetDetailsAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// <paramref name="salonName"/> is supplied by the caller, not
    /// re-fetched here - the confirmation-screen flow this backs always
    /// calls <see cref="GetDetailsAsync"/> first (to show "join {SalonName}
    /// as {Role}?"), so accepting would otherwise redundantly repeat that
    /// same lookup just to embed the name in the returned
    /// <see cref="AcceptedMembership"/>. ROJAN_Backend's own accept
    /// response carries only salon id and role, not the name.
    /// </summary>
    public Task<AcceptedMembership> AcceptAsync(string token, string salonName, CancellationToken cancellationToken = default);

    /// <summary>
    /// QR Ecosystem: creates a new staff invite for <paramref name="salonId"/>
    /// with the given <paramref name="role"/> - the owner-side counterpart
    /// to <see cref="GetDetailsAsync"/>/<see cref="AcceptAsync"/>'s
    /// invitee-side flow. Unlike those two, this one and
    /// <see cref="GetInviteQrCodeAsync"/> are salon-scoped by id, not
    /// token-scoped - the caller already has a real salon membership
    /// (an owner or a manager) to be creating an invite from.
    /// </summary>
    public Task<CreatedSalonInvite> CreateAsync(string salonId, SalonRole role, CancellationToken cancellationToken = default);

    /// <summary>QR Ecosystem: a scannable PNG encoding <paramref name="inviteId"/>'s real accept link (ROJAN_Backend's <c>GenerateSalonInviteQrCodeUseCase</c> builds the URL - this repository never constructs it client-side, same reasoning as <c>Salons.ISalonRepository.GetQrCodeAsync</c>).</summary>
    public Task<byte[]> GetInviteQrCodeAsync(string salonId, string inviteId, int sizePx, CancellationToken cancellationToken = default);
}
