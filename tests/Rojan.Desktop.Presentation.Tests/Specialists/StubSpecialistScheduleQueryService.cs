using Rojan.Desktop.Application.Specialists.Schedule;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

/// <summary>Phase 7.2.6 Shift Engine UI Activation - controllable <see cref="ISpecialistScheduleQueryService"/> test double, same "func-per-method, defaults to empty" shape as this test suite's other stub query services.</summary>
public sealed class StubSpecialistScheduleQueryService : ISpecialistScheduleQueryService
{
    public Func<string, Task<IReadOnlyList<WeeklyAvailabilityDto>>> WeeklyAvailability { get; set; } =
        _ => Task.FromResult<IReadOnlyList<WeeklyAvailabilityDto>>([]);

    public Func<string, Task<IReadOnlyList<ScheduleOverrideDto>>> Overrides { get; set; } =
        _ => Task.FromResult<IReadOnlyList<ScheduleOverrideDto>>([]);

    public Func<string, Task<IReadOnlyList<SpecialistLeaveDto>>> Leave { get; set; } =
        _ => Task.FromResult<IReadOnlyList<SpecialistLeaveDto>>([]);

    public Func<string, Task<IReadOnlyList<SpecialistBlockDto>>> Blocks { get; set; } =
        _ => Task.FromResult<IReadOnlyList<SpecialistBlockDto>>([]);

    public Task<IReadOnlyList<WeeklyAvailabilityDto>> GetWeeklyAvailabilityAsync(string specialistId, CancellationToken cancellationToken = default) =>
        WeeklyAvailability(specialistId);

    public Task<IReadOnlyList<ScheduleOverrideDto>> GetOverridesAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Overrides(specialistId);

    public Task<IReadOnlyList<SpecialistLeaveDto>> GetLeaveAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Leave(specialistId);

    public Task<IReadOnlyList<SpecialistBlockDto>> GetBlocksAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Blocks(specialistId);
}
