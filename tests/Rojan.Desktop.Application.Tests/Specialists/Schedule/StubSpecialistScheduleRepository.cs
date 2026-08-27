using DomainSchedule = Rojan.Desktop.Domain.Specialists.Schedule;

namespace Rojan.Desktop.Application.Tests.Specialists.Schedule;

/// <summary>Controllable <see cref="DomainSchedule.ISpecialistScheduleRepository"/> test double - records the last call made to each mutation method and returns a fixed value from each query, same "record + fixed return" shape as other Stub repositories in this test suite.</summary>
public sealed class StubSpecialistScheduleRepository : DomainSchedule.ISpecialistScheduleRepository
{
    public IReadOnlyList<DomainSchedule.WeeklyAvailability> WeeklyAvailability { get; set; } = [];

    public IReadOnlyList<DomainSchedule.ScheduleOverride> Overrides { get; set; } = [];

    public IReadOnlyList<DomainSchedule.SpecialistLeave> Leave { get; set; } = [];

    public IReadOnlyList<DomainSchedule.SpecialistBlock> Blocks { get; set; } = [];

    public (string SpecialistId, DayOfWeek DayOfWeek, IReadOnlyList<DomainSchedule.TimeInterval> Intervals)? LastSetWeeklyAvailabilityCall { get; private set; }

    public (string SpecialistId, DayOfWeek DayOfWeek)? LastRemoveWeeklyAvailabilityCall { get; private set; }

    public (string SpecialistId, DateOnly ScheduleDate, IReadOnlyList<DomainSchedule.TimeInterval> Intervals, string? Reason)? LastSetOverrideCall { get; private set; }

    public (string SpecialistId, string OverrideId)? LastRemoveOverrideCall { get; private set; }

    public (string SpecialistId, DateOnly StartDate, DateOnly EndDate, string? Reason)? LastCreateLeaveCall { get; private set; }

    public (string SpecialistId, string LeaveId)? LastRemoveLeaveCall { get; private set; }

    public (string SpecialistId, DateOnly ScheduleDate, DomainSchedule.TimeInterval Interval, string? Reason)? LastCreateBlockCall { get; private set; }

    public (string SpecialistId, string BlockId)? LastRemoveBlockCall { get; private set; }

    public Task<IReadOnlyList<DomainSchedule.WeeklyAvailability>> GetWeeklyAvailabilityAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult(WeeklyAvailability);

    public Task<DomainSchedule.WeeklyAvailability> SetWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, IReadOnlyList<DomainSchedule.TimeInterval> intervals, CancellationToken cancellationToken = default)
    {
        LastSetWeeklyAvailabilityCall = (specialistId, dayOfWeek, intervals);
        return Task.FromResult(new DomainSchedule.WeeklyAvailability("wa-1", specialistId, dayOfWeek, intervals));
    }

    public Task RemoveWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default)
    {
        LastRemoveWeeklyAvailabilityCall = (specialistId, dayOfWeek);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DomainSchedule.ScheduleOverride>> GetOverridesAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Overrides);

    public Task<DomainSchedule.ScheduleOverride> SetOverrideAsync(string specialistId, DateOnly scheduleDate, IReadOnlyList<DomainSchedule.TimeInterval> intervals, string? reason, CancellationToken cancellationToken = default)
    {
        LastSetOverrideCall = (specialistId, scheduleDate, intervals, reason);
        return Task.FromResult(new DomainSchedule.ScheduleOverride("ov-1", specialistId, scheduleDate, intervals, reason));
    }

    public Task RemoveOverrideAsync(string specialistId, string overrideId, CancellationToken cancellationToken = default)
    {
        LastRemoveOverrideCall = (specialistId, overrideId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DomainSchedule.SpecialistLeave>> GetLeaveAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Leave);

    public Task<DomainSchedule.SpecialistLeave> CreateLeaveAsync(string specialistId, DateOnly startDate, DateOnly endDate, string? reason, CancellationToken cancellationToken = default)
    {
        LastCreateLeaveCall = (specialistId, startDate, endDate, reason);
        return Task.FromResult(new DomainSchedule.SpecialistLeave("lv-1", specialistId, startDate, endDate, reason));
    }

    public Task RemoveLeaveAsync(string specialistId, string leaveId, CancellationToken cancellationToken = default)
    {
        LastRemoveLeaveCall = (specialistId, leaveId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DomainSchedule.SpecialistBlock>> GetBlocksAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Blocks);

    public Task<DomainSchedule.SpecialistBlock> CreateBlockAsync(string specialistId, DateOnly scheduleDate, DomainSchedule.TimeInterval interval, string? reason, CancellationToken cancellationToken = default)
    {
        LastCreateBlockCall = (specialistId, scheduleDate, interval, reason);
        return Task.FromResult(new DomainSchedule.SpecialistBlock("bl-1", specialistId, scheduleDate, interval, reason));
    }

    public Task RemoveBlockAsync(string specialistId, string blockId, CancellationToken cancellationToken = default)
    {
        LastRemoveBlockCall = (specialistId, blockId);
        return Task.CompletedTask;
    }
}
