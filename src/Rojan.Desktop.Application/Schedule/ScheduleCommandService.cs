namespace Rojan.Desktop.Application.Schedule;

/// <summary>Default <see cref="IScheduleCommandService"/> - thin pass-through to <see cref="IScheduleRepository"/>, no business rule of its own (every validation/conflict decision is Backend's), same shape as every other Command service in this app.</summary>
public sealed class ScheduleCommandService(IScheduleRepository repository) : IScheduleCommandService
{
    public Task<WeeklyAvailabilityDto> SetWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, IReadOnlyList<TimeIntervalDto> intervals, CancellationToken cancellationToken = default) =>
        repository.SetWeeklyAvailabilityAsync(specialistId, dayOfWeek, intervals, cancellationToken);

    public Task RemoveWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default) =>
        repository.RemoveWeeklyAvailabilityAsync(specialistId, dayOfWeek, cancellationToken);

    public Task<ScheduleOverrideDto> SetOverrideAsync(string specialistId, DateOnly scheduleDate, IReadOnlyList<TimeIntervalDto> intervals, string? reason, CancellationToken cancellationToken = default) =>
        repository.SetOverrideAsync(specialistId, scheduleDate, intervals, reason, cancellationToken);

    public Task RemoveOverrideAsync(string specialistId, string overrideId, CancellationToken cancellationToken = default) =>
        repository.RemoveOverrideAsync(specialistId, overrideId, cancellationToken);

    public Task<SpecialistLeaveDto> CreateLeaveAsync(string specialistId, DateOnly startDate, DateOnly endDate, string? reason, CancellationToken cancellationToken = default) =>
        repository.CreateLeaveAsync(specialistId, startDate, endDate, reason, cancellationToken);

    public Task RemoveLeaveAsync(string specialistId, string leaveId, CancellationToken cancellationToken = default) =>
        repository.RemoveLeaveAsync(specialistId, leaveId, cancellationToken);

    public Task<SpecialistBlockDto> CreateBlockAsync(string specialistId, DateOnly scheduleDate, TimeOnly start, TimeOnly endTime, string? reason, CancellationToken cancellationToken = default) =>
        repository.CreateBlockAsync(specialistId, scheduleDate, start, endTime, reason, cancellationToken);

    public Task RemoveBlockAsync(string specialistId, string blockId, CancellationToken cancellationToken = default) =>
        repository.RemoveBlockAsync(specialistId, blockId, cancellationToken);
}
