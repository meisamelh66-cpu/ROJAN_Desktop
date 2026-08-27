using Rojan.Desktop.Application.Specialists.Schedule;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

/// <summary>Phase 7.2.6 Shift Engine UI Activation - a no-op <see cref="ISpecialistScheduleQueryService"/> test double that always returns empty results, for tests that don't care about schedule data.</summary>
public sealed class EmptySpecialistScheduleQueryService : ISpecialistScheduleQueryService
{
    public Task<IReadOnlyList<WeeklyAvailabilityDto>> GetWeeklyAvailabilityAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WeeklyAvailabilityDto>>([]);

    public Task<IReadOnlyList<ScheduleOverrideDto>> GetOverridesAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ScheduleOverrideDto>>([]);

    public Task<IReadOnlyList<SpecialistLeaveDto>> GetLeaveAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SpecialistLeaveDto>>([]);

    public Task<IReadOnlyList<SpecialistBlockDto>> GetBlocksAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SpecialistBlockDto>>([]);
}
