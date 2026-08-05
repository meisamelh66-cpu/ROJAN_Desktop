namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>The response body <c>GET {ApiVersion.BasePath()}/salons/{salonId}/specialists/{specialistId}</c> returns - matches ROJAN_Backend's <c>SpecialistResponse</c> field-for-field (see <c>api/salon/SpecialistDtos.kt</c>), used by <c>BackendBookingRepository</c> to resolve a booking's display name for its specialist, and by <c>BackendSpecialistRepository</c> for the full catalog.</summary>
public sealed record SpecialistResponse(
    string Id,
    string SalonId,
    string? UserId,
    string DisplayName,
    string? Bio,
    string? PhotoUrl,
    bool Active);

/// <summary>The request body <c>POST {ApiVersion.BasePath()}/salons/{salonId}/specialists</c> accepts - matches ROJAN_Backend's <c>CreateSpecialistRequest</c> field-for-field. <c>BackendSpecialistRepository</c> always sends a null <see cref="UserId"/> - the Owner App has no field to supply one from.</summary>
public sealed record CreateSpecialistRequest(string? UserId, string DisplayName, string? Bio, string? PhotoUrl);

/// <summary>The request body <c>PUT {ApiVersion.BasePath()}/salons/{salonId}/specialists/{id}</c> accepts - matches ROJAN_Backend's <c>UpdateSpecialistRequest</c> field-for-field. Deliberately has no status/active field at all - see <c>BackendSpecialistRepository.UpdateSpecialistAsync</c>'s own doc comment for why a status change cannot be fulfilled through this endpoint.</summary>
public sealed record UpdateSpecialistRequest(string DisplayName, string? Bio, string? PhotoUrl);
