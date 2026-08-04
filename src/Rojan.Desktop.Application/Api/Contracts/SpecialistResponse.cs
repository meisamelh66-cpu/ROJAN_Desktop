namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>The response body <c>GET {ApiVersion.BasePath()}/salons/{salonId}/specialists/{specialistId}</c> returns - matches ROJAN_Backend's <c>SpecialistResponse</c> field-for-field (see <c>api/salon/SpecialistDtos.kt</c>), used by <c>BackendBookingRepository</c> to resolve a booking's display name for its specialist.</summary>
public sealed record SpecialistResponse(
    string Id,
    string SalonId,
    string? UserId,
    string DisplayName,
    string? Bio,
    string? PhotoUrl,
    bool Active);
