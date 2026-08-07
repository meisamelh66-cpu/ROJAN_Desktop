namespace Rojan.Desktop.Application.Membership;

/// <summary>Application-layer shape of an invite lookup, mapped from <see cref="Domain.Membership.SalonInviteDetails"/> by <see cref="SalonInviteMapper"/> - the only way Presentation ever reaches invite data, never through Domain/Infrastructure directly, same convention <c>Salons.SalonDto</c>/<c>SalonMapper</c> already establish.</summary>
public sealed record SalonInviteDetailsDto(string SalonName, string Role);

/// <summary>Application-layer shape of a successful accept, mapped from <see cref="Domain.Membership.AcceptedMembership"/>.</summary>
public sealed record AcceptedMembershipDto(string SalonId, string SalonName, string Role);
