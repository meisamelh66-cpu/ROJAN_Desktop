using Rojan.Desktop.Application.Specialists.Schedule;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

/// <summary>Phase 7.2.6 Shift Engine UI Activation - a no-op <see cref="ISpecialistScheduleCommandService"/> test double for tests that construct a profile ViewModel but never exercise its Schedule mutations.</summary>
public sealed class NoOpSpecialistScheduleCommandService : ISpecialistScheduleCommandService
{
    public Task<WeeklyAvailabilityDto> SetWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, IReadOnlyList<TimeIntervalDto> intervals, CancellationToken cancellationToken = default) =>
        Task.FromResult(new WeeklyAvailabilityDto("wa-1", specialistId, dayOfWeek, intervals));

    public Task RemoveWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<ScheduleOverrideDto> SetOverrideAsync(string specialistId, DateOnly scheduleDate, IReadOnlyList<TimeIntervalDto> intervals, string? reason, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ScheduleOverrideDto("ov-1", specialistId, scheduleDate, intervals, reason));

    public Task RemoveOverrideAsync(string specialistId, string overrideId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<SpecialistLeaveDto> CreateLeaveAsync(string specialistId, DateOnly startDate, DateOnly endDate, string? reason, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SpecialistLeaveDto("lv-1", specialistId, startDate, endDate, reason));

    public Task RemoveLeaveAsync(string specialistId, string leaveId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<SpecialistBlockDto> CreateBlockAsync(string specialistId, DateOnly scheduleDate, TimeIntervalDto interval, string? reason, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SpecialistBlockDto("bl-1", specialistId, scheduleDate, interval, reason));

    public Task RemoveBlockAsync(string specialistId, string blockId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
