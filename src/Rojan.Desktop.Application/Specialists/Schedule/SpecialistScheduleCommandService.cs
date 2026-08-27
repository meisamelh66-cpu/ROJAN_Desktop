using DomainSchedule = Rojan.Desktop.Domain.Specialists.Schedule;

namespace Rojan.Desktop.Application.Specialists.Schedule;

/// <summary>
/// Default <see cref="ISpecialistScheduleCommandService"/> implementation -
/// thin passthrough to <see cref="DomainSchedule.ISpecialistScheduleRepository"/>
/// plus Domain&lt;-&gt;Application mapping via <see cref="SpecialistScheduleMapper"/>.
/// No conflict validation, no permission check, no Calendar dependency -
/// all three deliberately excluded, see <see cref="DomainSchedule.ISpecialistScheduleRepository"/>'s
/// own doc comment. Permission enforcement is the caller's
/// responsibility, via <see cref="SpecialistScheduleCommandServicePermissionGate"/>
/// wrapping this class - same layering as every other Command service in
/// this codebase.
/// </summary>
public sealed class SpecialistScheduleCommandService : ISpecialistScheduleCommandService
{
    private readonly DomainSchedule.ISpecialistScheduleRepository _repository;

    public SpecialistScheduleCommandService(DomainSchedule.ISpecialistScheduleRepository repository)
    {
        _repository = repository;
    }

    public async Task<WeeklyAvailabilityDto> SetWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, IReadOnlyList<TimeIntervalDto> intervals, CancellationToken cancellationToken = default)
    {
        var domainIntervals = intervals.Select(SpecialistScheduleMapper.MapIntervalToDomain).ToList();
        var result = await _repository.SetWeeklyAvailabilityAsync(specialistId, dayOfWeek, domainIntervals, cancellationToken).ConfigureAwait(true);
        return SpecialistScheduleMapper.MapWeeklyAvailability(result);
    }

    public Task RemoveWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default) =>
        _repository.RemoveWeeklyAvailabilityAsync(specialistId, dayOfWeek, cancellationToken);

    public async Task<ScheduleOverrideDto> SetOverrideAsync(string specialistId, DateOnly scheduleDate, IReadOnlyList<TimeIntervalDto> intervals, string? reason, CancellationToken cancellationToken = default)
    {
        var domainIntervals = intervals.Select(SpecialistScheduleMapper.MapIntervalToDomain).ToList();
        var result = await _repository.SetOverrideAsync(specialistId, scheduleDate, domainIntervals, reason, cancellationToken).ConfigureAwait(true);
        return SpecialistScheduleMapper.MapOverride(result);
    }

    public Task RemoveOverrideAsync(string specialistId, string overrideId, CancellationToken cancellationToken = default) =>
        _repository.RemoveOverrideAsync(specialistId, overrideId, cancellationToken);

    public async Task<SpecialistLeaveDto> CreateLeaveAsync(string specialistId, DateOnly startDate, DateOnly endDate, string? reason, CancellationToken cancellationToken = default)
    {
        var result = await _repository.CreateLeaveAsync(specialistId, startDate, endDate, reason, cancellationToken).ConfigureAwait(true);
        return SpecialistScheduleMapper.MapLeave(result);
    }

    public Task RemoveLeaveAsync(string specialistId, string leaveId, CancellationToken cancellationToken = default) =>
        _repository.RemoveLeaveAsync(specialistId, leaveId, cancellationToken);

    public async Task<SpecialistBlockDto> CreateBlockAsync(string specialistId, DateOnly scheduleDate, TimeIntervalDto interval, string? reason, CancellationToken cancellationToken = default)
    {
        var result = await _repository.CreateBlockAsync(specialistId, scheduleDate, SpecialistScheduleMapper.MapIntervalToDomain(interval), reason, cancellationToken).ConfigureAwait(true);
        return SpecialistScheduleMapper.MapBlock(result);
    }

    public Task RemoveBlockAsync(string specialistId, string blockId, CancellationToken cancellationToken = default) =>
        _repository.RemoveBlockAsync(specialistId, blockId, cancellationToken);
}
