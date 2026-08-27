namespace Rojan.Desktop.Application.Specialists.Schedule;

/// <summary>Read side of the specialist-schedule vertical slice - no permission gate (matching every other <c>I*QueryService</c> in this codebase: reads are not permission-gated, only mutations are).</summary>
public interface ISpecialistScheduleQueryService
{
    public Task<IReadOnlyList<WeeklyAvailabilityDto>> GetWeeklyAvailabilityAsync(string specialistId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ScheduleOverrideDto>> GetOverridesAsync(string specialistId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<SpecialistLeaveDto>> GetLeaveAsync(string specialistId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<SpecialistBlockDto>> GetBlocksAsync(string specialistId, CancellationToken cancellationToken = default);
}
