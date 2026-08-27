using DomainSchedule = Rojan.Desktop.Domain.Specialists.Schedule;

namespace Rojan.Desktop.Application.Specialists.Schedule;

/// <summary>Default <see cref="ISpecialistScheduleQueryService"/> implementation - fetches from <see cref="DomainSchedule.ISpecialistScheduleRepository"/> and maps every Domain type to its Application-owned equivalent via <see cref="SpecialistScheduleMapper"/>, so nothing Domain-shaped ever crosses into Presentation - same pattern as <c>Specialists.SpecialistQueryService</c>.</summary>
public sealed class SpecialistScheduleQueryService : ISpecialistScheduleQueryService
{
    private readonly DomainSchedule.ISpecialistScheduleRepository _repository;

    public SpecialistScheduleQueryService(DomainSchedule.ISpecialistScheduleRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<WeeklyAvailabilityDto>> GetWeeklyAvailabilityAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        var availability = await _repository.GetWeeklyAvailabilityAsync(specialistId, cancellationToken).ConfigureAwait(true);
        return availability.Select(SpecialistScheduleMapper.MapWeeklyAvailability).ToList();
    }

    public async Task<IReadOnlyList<ScheduleOverrideDto>> GetOverridesAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        var overrides = await _repository.GetOverridesAsync(specialistId, cancellationToken).ConfigureAwait(true);
        return overrides.Select(SpecialistScheduleMapper.MapOverride).ToList();
    }

    public async Task<IReadOnlyList<SpecialistLeaveDto>> GetLeaveAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        var leave = await _repository.GetLeaveAsync(specialistId, cancellationToken).ConfigureAwait(true);
        return leave.Select(SpecialistScheduleMapper.MapLeave).ToList();
    }

    public async Task<IReadOnlyList<SpecialistBlockDto>> GetBlocksAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        var blocks = await _repository.GetBlocksAsync(specialistId, cancellationToken).ConfigureAwait(true);
        return blocks.Select(SpecialistScheduleMapper.MapBlock).ToList();
    }
}
