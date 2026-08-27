namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// The wire shapes ROJAN_Backend's <c>SpecialistScheduleController</c>
/// (<c>/api/v1/salons/{salonId}/specialists/{specialistId}/schedule/...</c>)
/// sends and accepts - matches <c>api/schedule/SpecialistScheduleDtos.kt</c>
/// field-for-field, verified directly against that file (Phase 7.2.4
/// readiness review), not assumed from the controller's method signatures
/// alone.
/// </summary>
/// <remarks>
/// <see cref="WeeklyAvailabilityResponse.DayOfWeek"/> is deliberately
/// <see cref="string"/>, not <see cref="System.DayOfWeek"/> - this
/// codebase's <c>HttpApiClient</c> uses <see cref="System.Text.Json.JsonSerializerDefaults.Web"/>
/// with no <c>JsonStringEnumConverter</c> registered anywhere (confirmed by
/// reading <c>HttpApiClient</c>'s own <c>SerializerOptions</c>), so a bare
/// C# enum property would fail to deserialize the JSON string Jackson
/// actually sends (e.g. <c>"MONDAY"</c>) - System.Text.Json's default enum
/// handling expects a number, not a name, with no converter present. Every
/// other enum-shaped field already in this codebase's wire contracts
/// (e.g. <see cref="SpecialistResponse.Active"/> as a plain <see cref="bool"/>)
/// avoids this the same way: receive the raw wire value, map explicitly in
/// <c>Specialists.Schedule.SpecialistScheduleMapper</c>, never trust
/// automatic enum deserialization.
/// </remarks>
public sealed record ScheduleTimeIntervalDto(TimeSpan Start, TimeSpan End);

public sealed record SetWeeklyAvailabilityRequest(IReadOnlyList<ScheduleTimeIntervalDto> Intervals);

public sealed record WeeklyAvailabilityResponse(
    string Id,
    string SpecialistId,
    string DayOfWeek,
    IReadOnlyList<ScheduleTimeIntervalDto> Intervals);

public sealed record SetScheduleOverrideRequest(DateOnly Date, IReadOnlyList<ScheduleTimeIntervalDto> Intervals, string? Reason);

public sealed record ScheduleOverrideResponse(
    string Id,
    string SpecialistId,
    DateOnly Date,
    IReadOnlyList<ScheduleTimeIntervalDto> Intervals,
    string? Reason);

public sealed record CreateLeaveRequest(DateOnly StartDate, DateOnly EndDate, string? Reason);

public sealed record LeaveResponse(
    string Id,
    string SpecialistId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Reason);

public sealed record CreateBlockRequest(DateOnly Date, TimeSpan Start, TimeSpan End, string? Reason);

public sealed record BlockResponse(
    string Id,
    string SpecialistId,
    DateOnly Date,
    TimeSpan Start,
    TimeSpan End,
    string? Reason);
