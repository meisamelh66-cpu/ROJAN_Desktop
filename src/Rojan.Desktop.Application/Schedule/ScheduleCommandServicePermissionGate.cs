using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Schedule;

/// <summary>
/// Phase 5 Shift Engine: wraps <see cref="IScheduleCommandService"/> with
/// real backend permission enforcement, built real from day one - same
/// "wrap the real service with IBackendPermissionGate" pattern
/// <c>Bookings.BookingCommandServicePermissionGate</c>/
/// <c>Calendar.CalendarCommandServicePermissionGate</c> already established,
/// never the legacy local <see cref="IPermissionGate"/>/<c>RolePermissions</c>
/// table.
///
/// ROJAN_Backend's own <c>SalonPermissionResolver.canManageSpecialist</c>
/// grants schedule-management either via <c>MANAGE_SCHEDULE_ALL</c> (owner -
/// who holds every permission - or a manager) or, for a specialist acting on
/// their own record only, <c>MANAGE_SCHEDULE_OWN</c>. This gate checks only
/// <c>MANAGE_SCHEDULE_ALL</c> - the <c>MANAGE_SCHEDULE_OWN</c>/"specialist
/// managing their own schedule" case is deliberately excluded, not silently
/// folded in, same "state the exclusion, don't guess at it" reasoning as
/// <c>BookingCommandServicePermissionGate</c>'s own doc comment: no real
/// Desktop session today resolves to a Specialist-role membership at all
/// (<c>SalonSessionAdapter.ToWorkspaceRole</c> only ever produces Owner/
/// Manager/Reception from a real backend <c>SalonRole</c>), so this
/// exclusion has no live consequence today, and stating it here means a
/// future session type that does reach this code won't be silently
/// mismatched against what the real backend would actually allow.
/// </summary>
public sealed class ScheduleCommandServicePermissionGate(IScheduleCommandService inner, IBackendPermissionGate backendPermissionGate) : IScheduleCommandService
{
    private const string ManageScheduleAll = "MANAGE_SCHEDULE_ALL";

    public Task<WeeklyAvailabilityDto> SetWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, IReadOnlyList<TimeIntervalDto> intervals, CancellationToken cancellationToken = default)
    {
        backendPermissionGate.EnsureBackend(ManageScheduleAll);
        return inner.SetWeeklyAvailabilityAsync(specialistId, dayOfWeek, intervals, cancellationToken);
    }

    public Task RemoveWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default)
    {
        backendPermissionGate.EnsureBackend(ManageScheduleAll);
        return inner.RemoveWeeklyAvailabilityAsync(specialistId, dayOfWeek, cancellationToken);
    }

    public Task<ScheduleOverrideDto> SetOverrideAsync(string specialistId, DateOnly scheduleDate, IReadOnlyList<TimeIntervalDto> intervals, string? reason, CancellationToken cancellationToken = default)
    {
        backendPermissionGate.EnsureBackend(ManageScheduleAll);
        return inner.SetOverrideAsync(specialistId, scheduleDate, intervals, reason, cancellationToken);
    }

    public Task RemoveOverrideAsync(string specialistId, string overrideId, CancellationToken cancellationToken = default)
    {
        backendPermissionGate.EnsureBackend(ManageScheduleAll);
        return inner.RemoveOverrideAsync(specialistId, overrideId, cancellationToken);
    }

    public Task<SpecialistLeaveDto> CreateLeaveAsync(string specialistId, DateOnly startDate, DateOnly endDate, string? reason, CancellationToken cancellationToken = default)
    {
        backendPermissionGate.EnsureBackend(ManageScheduleAll);
        return inner.CreateLeaveAsync(specialistId, startDate, endDate, reason, cancellationToken);
    }

    public Task RemoveLeaveAsync(string specialistId, string leaveId, CancellationToken cancellationToken = default)
    {
        backendPermissionGate.EnsureBackend(ManageScheduleAll);
        return inner.RemoveLeaveAsync(specialistId, leaveId, cancellationToken);
    }

    public Task<SpecialistBlockDto> CreateBlockAsync(string specialistId, DateOnly scheduleDate, TimeOnly start, TimeOnly endTime, string? reason, CancellationToken cancellationToken = default)
    {
        backendPermissionGate.EnsureBackend(ManageScheduleAll);
        return inner.CreateBlockAsync(specialistId, scheduleDate, start, endTime, reason, cancellationToken);
    }

    public Task RemoveBlockAsync(string specialistId, string blockId, CancellationToken cancellationToken = default)
    {
        backendPermissionGate.EnsureBackend(ManageScheduleAll);
        return inner.RemoveBlockAsync(specialistId, blockId, cancellationToken);
    }
}
