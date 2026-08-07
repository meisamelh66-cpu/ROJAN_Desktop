namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// The response body <c>GET /api/v1/salons/{salonId}/specialists/{specialistId}/available-slots</c>
/// returns - matches ROJAN_Backend's <c>TimeSlotResponse</c> field-for-field
/// (see <c>api/booking/AvailabilityController.kt</c>). <see cref="Start"/>/<see cref="End"/>
/// are plain <see cref="DateTime"/>, not <see cref="DateTimeOffset"/> - the
/// backend sends Kotlin <c>LocalDateTime</c> (no timezone offset), same
/// convention as <see cref="BookingResponse.StartTime"/>/<see cref="BookingResponse.EndTime"/>.
/// </summary>
public sealed record TimeSlotResponse(DateTime Start, DateTime End);
