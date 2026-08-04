namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// The response body every ROJAN_Backend booking endpoint returns - matches
/// that project's <c>BookingResponse</c> field-for-field (see
/// <c>api/booking/BookingDtos.kt</c>). <see cref="StartTime"/>/<see cref="EndTime"/>
/// are plain <see cref="DateTime"/>, not <see cref="DateTimeOffset"/> -
/// the backend sends Kotlin <c>LocalDateTime</c> (no timezone offset) for
/// these two fields specifically, unlike <see cref="CreatedAt"/>/<see cref="UpdatedAt"/>
/// (Kotlin <c>Instant</c>, real UTC, matches this namespace's existing
/// <see cref="DateTimeOffset"/> convention elsewhere). <see cref="Status"/>
/// stays a raw string (one of <c>PENDING/CONFIRMED/CANCELLED/COMPLETED</c>)
/// rather than a C# enum - mapped explicitly by the consuming repository,
/// same reasoning <see cref="AuthUserResponse.Role"/> already established.
/// </summary>
public sealed record BookingResponse(
    string Id,
    string SalonId,
    string ServiceId,
    string SpecialistId,
    string CustomerId,
    DateTime StartTime,
    DateTime EndTime,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>The request body <c>POST {ApiVersion.BasePath()}/bookings</c> accepts - matches ROJAN_Backend's <c>CreateBookingRequest</c> field-for-field. There is deliberately no <c>CustomerId</c> field - the backend always resolves the customer from the caller's own bearer token (see <c>BackendBookingRepository.CreateBookingAsync</c>'s own doc comment for why this makes owner-initiated creation unsupported today).</summary>
public sealed record CreateBookingRequest(string SalonId, string ServiceId, string SpecialistId, DateTime StartTime, string? Notes);

/// <summary>The request body <c>PUT {ApiVersion.BasePath()}/bookings/{id}/reschedule</c> accepts - matches ROJAN_Backend's <c>RescheduleBookingRequest</c> field-for-field.</summary>
public sealed record RescheduleBookingRequest(DateTime NewStartTime);
