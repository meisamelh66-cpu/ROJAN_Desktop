namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>The response body <c>GET /api/v1/invites/{token}</c> returns - matches ROJAN_Backend's <c>InviteDetailsResponse</c> field-for-field (see <c>api/invite/InviteDtos.kt</c>). <see cref="Role"/> is the raw backend string (<c>"MANAGER"</c>/<c>"RECEPTIONIST"</c>) - mapped to <c>WorkspaceRole</c> by whoever consumes it, not here, same "map backend strings once, at the boundary that needs the domain type" convention <c>BackendCustomerRepository.MapStatus</c> already uses.</summary>
public sealed record SalonInviteDetailsResponse(string SalonName, string Role);

/// <summary>The response body <c>POST /api/v1/invites/{token}/accept</c> returns - matches ROJAN_Backend's <c>SalonInviteAcceptedResponse</c> field-for-field.</summary>
public sealed record SalonInviteAcceptedResponse(string SalonId, string Role);
