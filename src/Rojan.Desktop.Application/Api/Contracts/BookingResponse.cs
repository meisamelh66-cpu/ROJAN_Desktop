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
///
/// TASK-003 (Walk-in Customer Booking): <see cref="CustomerId"/> became
/// nullable the moment the real backend started supporting walk-in bookings
/// (a booking for a CRM customer with no linked account) - exactly one of
/// <see cref="CustomerId"/>/<see cref="CrmCustomerId"/> is ever non-null now,
/// never both, never neither. <see cref="CrmCustomerId"/> is appended last
/// (with a default) rather than inserted next to <see cref="CustomerId"/> so
/// every existing positional construction of this record keeps compiling
/// unchanged.
/// </summary>
public sealed record BookingResponse(
    string Id,
    string SalonId,
    string ServiceId,
    string SpecialistId,
    string? CustomerId,
    DateTime StartTime,
    DateTime EndTime,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? CrmCustomerId = null);

/// <summary>The request body <c>POST {ApiVersion.BasePath()}/bookings</c> accepts - matches ROJAN_Backend's <c>CreateBookingRequest</c> field-for-field. There is deliberately no <c>CustomerId</c> field - the backend always resolves the customer from the caller's own bearer token (see <c>BackendBookingRepository.CreateBookingAsync</c>'s own doc comment for why this makes owner-initiated creation unsupported today).</summary>
public sealed record CreateBookingRequest(string SalonId, string ServiceId, string SpecialistId, DateTime StartTime, string? Notes);

/// <summary>The request body <c>PUT {ApiVersion.BasePath()}/bookings/{id}/reschedule</c> accepts - matches ROJAN_Backend's <c>RescheduleBookingRequest</c> field-for-field.</summary>
public sealed record RescheduleBookingRequest(DateTime NewStartTime);

/// <summary>
/// The request body <c>POST /api/v1/salons/{salonId}/bookings</c> accepts -
/// matches ROJAN_Backend's <c>CreateBookingForCustomerRequest</c>
/// field-for-field (see <c>api/booking/BookingDtos.kt</c>). The
/// owner-authorized counterpart to <see cref="CreateBookingRequest"/>:
/// <see cref="CustomerId"/> here is a Customer CRM id (from
/// <c>GET .../customers</c>), not a User id. TASK-003 (Walk-in Customer
/// Booking): the target customer no longer needs a linked account - a
/// customer with none becomes a real walk-in booking (response
/// <c>CustomerId</c> null, <c>CrmCustomerId</c> set), never a 409 any more -
/// see <c>BackendBookingRepository.CreateBookingAsync</c>'s own doc comment.
/// </summary>
public sealed record CreateBookingForCustomerRequest(string CustomerId, string ServiceId, string SpecialistId, DateTime StartTime, string? Notes);
