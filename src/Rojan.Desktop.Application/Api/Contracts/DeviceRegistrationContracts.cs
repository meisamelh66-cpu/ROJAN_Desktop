namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// Phase B: Windows Reception Device Registration. The request body
/// <c>POST {ApiVersion.BasePath()}/users/me/devices</c> accepts - matches
/// ROJAN_Backend's <c>RegisterDeviceRequest</c> field-for-field (see
/// <c>api/device/DeviceDtos.kt</c>). No user id field - the backend derives
/// the caller from the JWT/security context, never from the request body
/// (same reasoning <see cref="LoginRequest"/>/<see cref="OtpVerifyRequest"/>
/// already establish for every other authenticated write in this app).
/// <see cref="SalonId"/> is only ever a claim to be verified server-side
/// (ROJAN_Backend's <c>SalonPermissionResolver</c> re-resolves the caller's
/// real access to it) - this record itself does not, and cannot, enforce
/// that.
/// </summary>
public sealed record DeviceRegistrationRequest(string SalonId, string DeviceId, string? Fingerprint, string? InstallationId);

/// <summary>The response body <c>POST .../users/me/devices</c> returns - matches ROJAN_Backend's <c>AuthorizedDeviceResponse</c> field-for-field. Never carries a secret - <see cref="DeviceRegistrationRequest.Fingerprint"/>/<see cref="DeviceRegistrationRequest.InstallationId"/> are write-only from this client's perspective, not echoed back.</summary>
public sealed record DeviceRegistrationResponse(
    string Id,
    string SalonId,
    string DeviceId,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset? RevokedAt);
