namespace Rojan.Desktop.Domain.Specialists.Schedule;

/// <summary>
/// Repository abstraction for a specialist's own availability - the
/// Official Shift definition ("specialist assigned to availability
/// window"), backed by ROJAN_Backend's <c>SpecialistScheduleController</c>.
/// Deliberately covers all four resource groups (weekly availability,
/// overrides, leave, blocks) as one cohesive interface rather than four
/// separate ones - they share one specialist-scoped aggregate and one
/// permission model, the same "multi-resource, single-aggregate-root"
/// shape <see cref="Organizations.IOrganizationRepository"/> already
/// established for Organization/Branch.
///
/// This is a "dumb, no aggregation logic" repository, same convention as
/// every other Backend-connected repository in this codebase - it must
/// never validate conflicts (no overlap/conflict rule exists in
/// ROJAN_Backend today; inventing one here would be exactly the local
/// authority this codebase's own Booking/Calendar governance work exists
/// to prevent elsewhere), must never own a permission decision (that stays
/// in <c>Application.Specialists.Schedule.SpecialistScheduleCommandServicePermissionGate</c>,
/// one layer up), and must never depend on <c>Domain.Calendar</c> in
/// either direction - this repository is additive and standalone, no part
/// of how booking availability is read or computed.
/// </summary>
public interface ISpecialistScheduleRepository
{
    public Task<IReadOnlyList<WeeklyAvailability>> GetWeeklyAvailabilityAsync(string specialistId, CancellationToken cancellationToken = default);

    public Task<WeeklyAvailability> SetWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, IReadOnlyList<TimeInterval> intervals, CancellationToken cancellationToken = default);

    public Task RemoveWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ScheduleOverride>> GetOverridesAsync(string specialistId, CancellationToken cancellationToken = default);

    public Task<ScheduleOverride> SetOverrideAsync(string specialistId, DateOnly scheduleDate, IReadOnlyList<TimeInterval> intervals, string? reason, CancellationToken cancellationToken = default);

    public Task RemoveOverrideAsync(string specialistId, string overrideId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<SpecialistLeave>> GetLeaveAsync(string specialistId, CancellationToken cancellationToken = default);

    public Task<SpecialistLeave> CreateLeaveAsync(string specialistId, DateOnly startDate, DateOnly endDate, string? reason, CancellationToken cancellationToken = default);

    public Task RemoveLeaveAsync(string specialistId, string leaveId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<SpecialistBlock>> GetBlocksAsync(string specialistId, CancellationToken cancellationToken = default);

    public Task<SpecialistBlock> CreateBlockAsync(string specialistId, DateOnly scheduleDate, TimeInterval interval, string? reason, CancellationToken cancellationToken = default);

    public Task RemoveBlockAsync(string specialistId, string blockId, CancellationToken cancellationToken = default);
}
