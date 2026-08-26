namespace Rojan.Desktop.Application.Schedule;

/// <summary>Write surface for a specialist's real schedule - see <see cref="IScheduleRepository"/>'s own doc comment for the authority reasoning.</summary>
public interface IScheduleCommandService
{
    public Task<WeeklyAvailabilityDto> SetWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, IReadOnlyList<TimeIntervalDto> intervals, CancellationToken cancellationToken = default);

    public Task RemoveWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default);

    public Task<ScheduleOverrideDto> SetOverrideAsync(string specialistId, DateOnly scheduleDate, IReadOnlyList<TimeIntervalDto> intervals, string? reason, CancellationToken cancellationToken = default);

    public Task RemoveOverrideAsync(string specialistId, string overrideId, CancellationToken cancellationToken = default);

    public Task<SpecialistLeaveDto> CreateLeaveAsync(string specialistId, DateOnly startDate, DateOnly endDate, string? reason, CancellationToken cancellationToken = default);

    public Task RemoveLeaveAsync(string specialistId, string leaveId, CancellationToken cancellationToken = default);

    public Task<SpecialistBlockDto> CreateBlockAsync(string specialistId, DateOnly scheduleDate, TimeOnly start, TimeOnly endTime, string? reason, CancellationToken cancellationToken = default);

    public Task RemoveBlockAsync(string specialistId, string blockId, CancellationToken cancellationToken = default);
}
