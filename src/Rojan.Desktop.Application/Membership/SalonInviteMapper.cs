using DomainMembership = Rojan.Desktop.Domain.Membership;

namespace Rojan.Desktop.Application.Membership;

/// <summary>Domain&lt;-&gt;Application mapping used by <see cref="SalonInviteService"/> - same reasoning as <c>Salons.SalonMapper</c>.</summary>
internal static class SalonInviteMapper
{
    public static SalonInviteDetailsDto MapDetails(DomainMembership.SalonInviteDetails details) => new(details.SalonName, details.Role);

    public static AcceptedMembershipDto MapMembership(DomainMembership.AcceptedMembership membership) => new(membership.SalonId, membership.SalonName, membership.Role);
}
