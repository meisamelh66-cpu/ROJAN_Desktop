using DomainMembership = Rojan.Desktop.Domain.Membership;

namespace Rojan.Desktop.Application.Membership;

/// <summary>Default <see cref="ISalonInviteService"/> implementation.</summary>
public sealed class SalonInviteService(DomainMembership.ISalonInviteRepository repository, IAcceptedMembershipStore membershipStore) : ISalonInviteService
{
    public async Task<SalonInviteDetailsDto> GetDetailsAsync(string token, CancellationToken cancellationToken = default)
    {
        var details = await repository.GetDetailsAsync(token, cancellationToken).ConfigureAwait(true);
        return SalonInviteMapper.MapDetails(details);
    }

    public async Task<AcceptedMembershipDto> AcceptAsync(string token, string salonName, CancellationToken cancellationToken = default)
    {
        var membership = await repository.AcceptAsync(token, salonName, cancellationToken).ConfigureAwait(true);
        await membershipStore.SaveAsync(membership, cancellationToken).ConfigureAwait(true);
        return SalonInviteMapper.MapMembership(membership);
    }
}
