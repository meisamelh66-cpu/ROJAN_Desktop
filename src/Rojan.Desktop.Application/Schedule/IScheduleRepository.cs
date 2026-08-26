namespace Rojan.Desktop.Application.Schedule;

/// <summary>
/// Repository abstraction for a specialist's real schedule -
/// ROJAN_Backend's <c>SpecialistScheduleController</c> is the only
/// authority for this data (weekly availability, one-off overrides, leave,
/// ad-hoc blocks); Desktop never generates, calculates, or stores any of
/// it locally. No Fake implementation exists for this module - unlike
/// every earlier vertical slice in this app, Shift Engine was built real
/// from day one against an already-existing, already-verified real
/// Backend contract, so there was never a local-authority phase to
/// migrate away from.
/// </summary>
public interface IScheduleRepository
{
    public Task<IReadOnlyList<WeeklyAvailabilityDto>> GetWeeklyAvailabilityAsync(string specialistId, CancellationToken cancellationToken = default);

    public Task<WeeklyAvailabilityDto> SetWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, IReadOnlyList<TimeIntervalDto> intervals, CancellationToken cancellationToken = default);

    public Task RemoveWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ScheduleOverrideDto>> GetOverridesAsync(string specialistId, CancellationToken cancellationToken = default);

    public Task<ScheduleOverrideDto> SetOverrideAsync(string specialistId, DateOnly scheduleDate, IReadOnlyList<TimeIntervalDto> intervals, string? reason, CancellationToken cancellationToken = default);

    public Task RemoveOverrideAsync(string specialistId, string overrideId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<SpecialistLeaveDto>> GetLeavesAsync(string specialistId, CancellationToken cancellationToken = default);

    public Task<SpecialistLeaveDto> CreateLeaveAsync(string specialistId, DateOnly startDate, DateOnly endDate, string? reason, CancellationToken cancellationToken = default);

    public Task RemoveLeaveAsync(string specialistId, string leaveId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<SpecialistBlockDto>> GetBlocksAsync(string specialistId, CancellationToken cancellationToken = default);

    public Task<SpecialistBlockDto> CreateBlockAsync(string specialistId, DateOnly scheduleDate, TimeOnly start, TimeOnly endTime, string? reason, CancellationToken cancellationToken = default);

    public Task RemoveBlockAsync(string specialistId, string blockId, CancellationToken cancellationToken = default);
}
