namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// The response body <c>GET {ApiVersion.BasePath()}/salons/{salonId}/media</c>
/// returns a list of - matches ROJAN_Backend's <c>MediaAssetResponse</c>
/// field-for-field (see <c>api/media/MediaDtos.kt</c>).
/// </summary>
/// <remarks>
/// <see cref="MediaType"/>/<see cref="Status"/> are deliberately
/// <see cref="string"/>, not C# enums - same reasoning as
/// <c>SpecialistScheduleContracts.WeeklyAvailabilityResponse.DayOfWeek</c>'s
/// own doc comment: this codebase's <c>HttpApiClient</c> registers no
/// <c>JsonStringEnumConverter</c>, so a bare enum property would fail to
/// deserialize the JSON string names ROJAN_Backend actually sends (e.g.
/// <c>"LOGO"</c>). Mapped explicitly in
/// the Infrastructure-layer <c>BackendSalonMediaRepository</c>, never
/// trusted to automatic enum deserialization.
/// </remarks>
public sealed record MediaAssetResponse(
    string Id,
    string SalonId,
    string MediaType,
    string OriginalName,
    string MimeType,
    long FileSize,
    string Status,
    string Url,
    DateTimeOffset CreatedAt,
    string? TargetId,
    int DisplayOrder);
