using Rojan.Desktop.Application.Schedule;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

/// <summary>Empty-by-default stub - most SpecialistProfileViewModel tests don't exercise Schedule at all, same "safe no-op default" reasoning as this file's sibling stubs elsewhere in this app.</summary>
internal sealed class StubScheduleQueryService : IScheduleQueryService
{
    public Task<IReadOnlyList<WeeklyAvailabilityDto>> GetWeeklyAvailabilityAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WeeklyAvailabilityDto>>([]);

    public Task<IReadOnlyList<ScheduleOverrideDto>> GetOverridesAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ScheduleOverrideDto>>([]);

    public Task<IReadOnlyList<SpecialistLeaveDto>> GetLeavesAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SpecialistLeaveDto>>([]);

    public Task<IReadOnlyList<SpecialistBlockDto>> GetBlocksAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SpecialistBlockDto>>([]);
}

/// <summary>Records every call it receives, same reasoning as StubSpecialistCommandService - not exercised by most SpecialistProfileViewModel tests, kept minimal.</summary>
internal sealed class StubScheduleCommandService : IScheduleCommandService
{
    public Task<WeeklyAvailabilityDto> SetWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, IReadOnlyList<TimeIntervalDto> intervals, CancellationToken cancellationToken = default) =>
        Task.FromResult(new WeeklyAvailabilityDto("new-availability", specialistId, dayOfWeek, intervals, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

    public Task RemoveWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<ScheduleOverrideDto> SetOverrideAsync(string specialistId, DateOnly scheduleDate, IReadOnlyList<TimeIntervalDto> intervals, string? reason, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ScheduleOverrideDto("new-override", specialistId, scheduleDate, intervals, reason, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

    public Task RemoveOverrideAsync(string specialistId, string overrideId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<SpecialistLeaveDto> CreateLeaveAsync(string specialistId, DateOnly startDate, DateOnly endDate, string? reason, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SpecialistLeaveDto("new-leave", specialistId, startDate, endDate, reason, DateTimeOffset.UtcNow));

    public Task RemoveLeaveAsync(string specialistId, string leaveId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<SpecialistBlockDto> CreateBlockAsync(string specialistId, DateOnly scheduleDate, TimeOnly start, TimeOnly endTime, string? reason, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SpecialistBlockDto("new-block", specialistId, scheduleDate, start, endTime, reason, DateTimeOffset.UtcNow));

    public Task RemoveBlockAsync(string specialistId, string blockId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
