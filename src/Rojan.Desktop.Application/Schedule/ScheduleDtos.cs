namespace Rojan.Desktop.Application.Schedule;

/// <summary>One [start, end) window within a day - the Application-layer shape every Schedule DTO below carries its intervals as.</summary>
public sealed record TimeIntervalDto(TimeOnly Start, TimeOnly End);

/// <summary>A specialist's recurring weekly availability for one real .NET <see cref="System.DayOfWeek"/> - Backend authority, real (<c>SpecialistScheduleController</c>), never computed or fabricated here.</summary>
public sealed record WeeklyAvailabilityDto(string Id, string SpecialistId, DayOfWeek DayOfWeek, IReadOnlyList<TimeIntervalDto> Intervals, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

/// <summary>A one-off override of a specialist's availability for a specific date - real, Backend-authoritative. An empty <see cref="Intervals"/> list is a real, intentional "fully unavailable this date" override, not an error or a loading state.</summary>
public sealed record ScheduleOverrideDto(string Id, string SpecialistId, DateOnly Date, IReadOnlyList<TimeIntervalDto> Intervals, string? Reason, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

/// <summary>A specialist's vacation/leave date range - real, Backend-authoritative. <see cref="Reason"/> is <see langword="null"/> whenever Backend itself redacted it for this viewer (see <c>ScheduleOverrideResponse</c>'s own doc comment) - never distinguished from "no reason given" here, since Desktop has no way to tell the two apart and must not guess.</summary>
public sealed record SpecialistLeaveDto(string Id, string SpecialistId, DateOnly StartDate, DateOnly EndDate, string? Reason, DateTimeOffset CreatedAt);

/// <summary>An ad-hoc blocked time window for a specialist (e.g. a mid-day appointment elsewhere) - real, Backend-authoritative. Same redaction caveat on <see cref="Reason"/> as <see cref="SpecialistLeaveDto"/>.</summary>
public sealed record SpecialistBlockDto(string Id, string SpecialistId, DateOnly Date, TimeOnly Start, TimeOnly End, string? Reason, DateTimeOffset CreatedAt);
