namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>The response body <c>GET /api/v1/invites/{token}</c> returns - matches ROJAN_Backend's <c>InviteDetailsResponse</c> field-for-field (see <c>api/invite/InviteDtos.kt</c>). <see cref="Role"/> is the raw backend string (<c>"MANAGER"</c>/<c>"RECEPTIONIST"</c>) - mapped to <c>WorkspaceRole</c> by whoever consumes it, not here, same "map backend strings once, at the boundary that needs the domain type" convention <c>BackendCustomerRepository.MapStatus</c> already uses.</summary>
public sealed record SalonInviteDetailsResponse(string SalonName, string Role);

/// <summary>The response body <c>POST /api/v1/invites/{token}/accept</c> returns - matches ROJAN_Backend's <c>SalonInviteAcceptedResponse</c> field-for-field.</summary>
public sealed record SalonInviteAcceptedResponse(string SalonId, string Role);

/// <summary>QR Ecosystem (Desktop Productionization Sprint 1): the request body <c>POST /api/v1/salons/{salonId}/invites</c> expects - matches ROJAN_Backend's <c>CreateSalonInviteRequest</c> (<c>api/salon/SalonInviteDtos.kt</c>) field-for-field. <see cref="Role"/> is the raw backend enum name (<c>"MANAGER"</c>/<c>"RECEPTIONIST"</c>) - <c>Infrastructure.Membership.BackendSalonInviteRepository</c> maps <see cref="Domain.Membership.SalonRole"/> to it, same "map at the boundary" convention every other backend enum crossing here already follows.</summary>
public sealed record CreateSalonInviteRequest(string Role);

/// <summary>The response body <c>POST /api/v1/salons/{salonId}/invites</c> returns - only the two fields this app needs today (<c>Id</c> to immediately request the invite's QR code) are modeled; ROJAN_Backend's full <c>SalonInviteResponse</c> also carries status/expiry/createdBy/etc., irrelevant until a future invite-list UI needs them (a documented, not silent, scope boundary - see <c>Domain.Membership.CreatedSalonInvite</c>'s own doc comment).</summary>
public sealed record CreateSalonInviteResponse(string Id, string Token);
