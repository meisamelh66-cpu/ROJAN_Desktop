using Rojan.Desktop.Domain.Membership;

namespace Rojan.Desktop.Application.Membership;

/// <summary>
/// Local persistence for the current signed-in user's accepted Salon
/// Invite, if any. ROJAN_Backend has no "list my memberships" endpoint -
/// invite-accept returns <see cref="AcceptedMembership"/> exactly once,
/// synchronously - so this is what makes a Reception session survive a
/// later app restart without re-accepting an already-consumed, single-use
/// token. A device wipe or fresh install with nothing persisted here means
/// "accept a new invite" is the only recovery path - a known, deliberate
/// limitation (the owner can regenerate an invite cheaply), not solved
/// further by this store.
/// </summary>
public interface IAcceptedMembershipStore
{
    public Task<AcceptedMembership?> GetAsync(CancellationToken cancellationToken = default);

    public Task SaveAsync(AcceptedMembership membership, CancellationToken cancellationToken = default);

    public Task ClearAsync(CancellationToken cancellationToken = default);
}
