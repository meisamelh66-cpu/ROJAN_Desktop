namespace Rojan.Desktop.Application.Schedule;

/// <summary>Default <see cref="IScheduleQueryService"/> - thin pass-through to <see cref="IScheduleRepository"/>, no aggregation or filtering of its own, same shape as every other Query service in this app.</summary>
public sealed class ScheduleQueryService(IScheduleRepository repository) : IScheduleQueryService
{
    public Task<IReadOnlyList<WeeklyAvailabilityDto>> GetWeeklyAvailabilityAsync(string specialistId, CancellationToken cancellationToken = default) =>
        repository.GetWeeklyAvailabilityAsync(specialistId, cancellationToken);

    public Task<IReadOnlyList<ScheduleOverrideDto>> GetOverridesAsync(string specialistId, CancellationToken cancellationToken = default) =>
        repository.GetOverridesAsync(specialistId, cancellationToken);

    public Task<IReadOnlyList<SpecialistLeaveDto>> GetLeavesAsync(string specialistId, CancellationToken cancellationToken = default) =>
        repository.GetLeavesAsync(specialistId, cancellationToken);

    public Task<IReadOnlyList<SpecialistBlockDto>> GetBlocksAsync(string specialistId, CancellationToken cancellationToken = default) =>
        repository.GetBlocksAsync(specialistId, cancellationToken);
}
