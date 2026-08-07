namespace Rojan.Desktop.Domain.Membership;

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
}
