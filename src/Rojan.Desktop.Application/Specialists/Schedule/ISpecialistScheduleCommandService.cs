namespace Rojan.Desktop.Application.Specialists.Schedule;

/// <summary>Write side of the specialist-schedule vertical slice - every method is permission-gated by <see cref="SpecialistScheduleCommandServicePermissionGate"/> in front of <see cref="SpecialistScheduleCommandService"/>.</summary>
public interface ISpecialistScheduleCommandService
{
    public Task<WeeklyAvailabilityDto> SetWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, IReadOnlyList<TimeIntervalDto> intervals, CancellationToken cancellationToken = default);

    public Task RemoveWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default);

    public Task<ScheduleOverrideDto> SetOverrideAsync(string specialistId, DateOnly scheduleDate, IReadOnlyList<TimeIntervalDto> intervals, string? reason, CancellationToken cancellationToken = default);

    public Task RemoveOverrideAsync(string specialistId, string overrideId, CancellationToken cancellationToken = default);

    public Task<SpecialistLeaveDto> CreateLeaveAsync(string specialistId, DateOnly startDate, DateOnly endDate, string? reason, CancellationToken cancellationToken = default);

    public Task RemoveLeaveAsync(string specialistId, string leaveId, CancellationToken cancellationToken = default);

    public Task<SpecialistBlockDto> CreateBlockAsync(string specialistId, DateOnly scheduleDate, TimeIntervalDto interval, string? reason, CancellationToken cancellationToken = default);

    public Task RemoveBlockAsync(string specialistId, string blockId, CancellationToken cancellationToken = default);
}
