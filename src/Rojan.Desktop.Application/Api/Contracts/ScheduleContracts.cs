namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>One [start, end) window within a day - matches ROJAN_Backend's <c>TimeIntervalDto</c> field-for-field (see <c>api/schedule/SpecialistScheduleDtos.kt</c>/<c>WorkingHoursDtos.kt</c>).</summary>
public sealed record TimeIntervalDto(TimeOnly Start, TimeOnly End);

/// <summary>The request body <c>PUT .../schedule/weekly-availability/{dayOfWeek}</c> accepts - matches ROJAN_Backend's <c>SetWeeklyAvailabilityRequest</c> field-for-field.</summary>
public sealed record SetWeeklyAvailabilityRequest(IReadOnlyList<TimeIntervalDto> Intervals);

/// <summary>The response body ROJAN_Backend's <c>SpecialistScheduleController</c> weekly-availability endpoints return - matches its <c>WeeklyAvailabilityResponse</c> field-for-field. <see cref="DayOfWeek"/> stays a raw string (one of the real Java <c>DayOfWeek</c> enum names, e.g. <c>"MONDAY"</c>) rather than a C# enum on the wire type - mapped explicitly by the consuming repository, same reasoning <see cref="BookingResponse.Status"/> already established.</summary>
public sealed record WeeklyAvailabilityResponse(string Id, string SpecialistId, string DayOfWeek, IReadOnlyList<TimeIntervalDto> Intervals, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

/// <summary>The request body <c>PUT .../schedule/overrides/{date}</c> accepts - matches ROJAN_Backend's <c>SetScheduleOverrideRequest</c> field-for-field. An empty <see cref="Intervals"/> list means the specialist is fully unavailable that date - a real, intentional override, not an error.</summary>
public sealed record SetScheduleOverrideRequest(IReadOnlyList<TimeIntervalDto> Intervals, string? Reason);

/// <summary>The response body ROJAN_Backend's schedule-override endpoints return - matches its <c>ScheduleOverrideResponse</c> field-for-field. <see cref="Reason"/> is redacted to <see langword="null"/> by the backend itself for any caller who isn't the salon owner or doesn't hold <c>MANAGE_SCHEDULE_ALL</c> for this specialist - Desktop never needs its own redaction logic, it only ever sees what Backend already decided to disclose.</summary>
public sealed record ScheduleOverrideResponse(string Id, string SpecialistId, DateOnly Date, IReadOnlyList<TimeIntervalDto> Intervals, string? Reason, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

/// <summary>The request body <c>POST .../schedule/leaves</c> accepts - matches ROJAN_Backend's <c>CreateLeaveRequest</c> field-for-field.</summary>
public sealed record CreateLeaveRequest(DateOnly StartDate, DateOnly EndDate, string? Reason);

/// <summary>The response body ROJAN_Backend's leave endpoints return - matches its <c>LeaveResponse</c> field-for-field. Same real, backend-only <see cref="Reason"/> redaction as <see cref="ScheduleOverrideResponse"/>.</summary>
public sealed record LeaveResponse(string Id, string SpecialistId, DateOnly StartDate, DateOnly EndDate, string? Reason, DateTimeOffset CreatedAt);

/// <summary>The request body <c>POST .../schedule/blocks</c> accepts - matches ROJAN_Backend's <c>CreateBlockRequest</c> field-for-field.</summary>
public sealed record CreateBlockRequest(DateOnly Date, TimeOnly Start, TimeOnly End, string? Reason);

/// <summary>The response body ROJAN_Backend's block endpoints return - matches its <c>BlockResponse</c> field-for-field. Same real, backend-only <see cref="Reason"/> redaction as <see cref="ScheduleOverrideResponse"/>.</summary>
public sealed record BlockResponse(string Id, string SpecialistId, DateOnly Date, TimeOnly Start, TimeOnly End, string? Reason, DateTimeOffset CreatedAt);
