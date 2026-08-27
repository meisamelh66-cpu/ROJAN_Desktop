using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Specialists.Schedule;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

/// <summary>Phase 7.2.6 Shift Engine UI Activation - controllable <see cref="ISpecialistScheduleCommandService"/> test double - records every mutation call and can be told to throw (e.g. <see cref="UnauthorizedOperationException"/>) via <see cref="Fail"/>.</summary>
public sealed class StubSpecialistScheduleCommandService : ISpecialistScheduleCommandService
{
    public Exception? Fail { get; set; }

    public int SetWeeklyAvailabilityCallCount { get; private set; }

    public int RemoveWeeklyAvailabilityCallCount { get; private set; }

    public int SetOverrideCallCount { get; private set; }

    public int RemoveOverrideCallCount { get; private set; }

    public int CreateLeaveCallCount { get; private set; }

    public int RemoveLeaveCallCount { get; private set; }

    public int CreateBlockCallCount { get; private set; }

    public int RemoveBlockCallCount { get; private set; }

    public Task<WeeklyAvailabilityDto> SetWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, IReadOnlyList<TimeIntervalDto> intervals, CancellationToken cancellationToken = default)
    {
        SetWeeklyAvailabilityCallCount++;
        return Fail is not null ? Task.FromException<WeeklyAvailabilityDto>(Fail) : Task.FromResult(new WeeklyAvailabilityDto("wa-1", specialistId, dayOfWeek, intervals));
    }

    public Task RemoveWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default)
    {
        RemoveWeeklyAvailabilityCallCount++;
        return Fail is not null ? Task.FromException(Fail) : Task.CompletedTask;
    }

    public Task<ScheduleOverrideDto> SetOverrideAsync(string specialistId, DateOnly scheduleDate, IReadOnlyList<TimeIntervalDto> intervals, string? reason, CancellationToken cancellationToken = default)
    {
        SetOverrideCallCount++;
        return Fail is not null ? Task.FromException<ScheduleOverrideDto>(Fail) : Task.FromResult(new ScheduleOverrideDto("ov-1", specialistId, scheduleDate, intervals, reason));
    }

    public Task RemoveOverrideAsync(string specialistId, string overrideId, CancellationToken cancellationToken = default)
    {
        RemoveOverrideCallCount++;
        return Fail is not null ? Task.FromException(Fail) : Task.CompletedTask;
    }

    public Task<SpecialistLeaveDto> CreateLeaveAsync(string specialistId, DateOnly startDate, DateOnly endDate, string? reason, CancellationToken cancellationToken = default)
    {
        CreateLeaveCallCount++;
        return Fail is not null ? Task.FromException<SpecialistLeaveDto>(Fail) : Task.FromResult(new SpecialistLeaveDto("lv-1", specialistId, startDate, endDate, reason));
    }

    public Task RemoveLeaveAsync(string specialistId, string leaveId, CancellationToken cancellationToken = default)
    {
        RemoveLeaveCallCount++;
        return Fail is not null ? Task.FromException(Fail) : Task.CompletedTask;
    }

    public Task<SpecialistBlockDto> CreateBlockAsync(string specialistId, DateOnly scheduleDate, TimeIntervalDto interval, string? reason, CancellationToken cancellationToken = default)
    {
        CreateBlockCallCount++;
        return Fail is not null ? Task.FromException<SpecialistBlockDto>(Fail) : Task.FromResult(new SpecialistBlockDto("bl-1", specialistId, scheduleDate, interval, reason));
    }

    public Task RemoveBlockAsync(string specialistId, string blockId, CancellationToken cancellationToken = default)
    {
        RemoveBlockCallCount++;
        return Fail is not null ? Task.FromException(Fail) : Task.CompletedTask;
    }
}
