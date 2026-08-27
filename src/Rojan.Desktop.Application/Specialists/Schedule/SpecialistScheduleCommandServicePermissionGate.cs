using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Specialists.Schedule;

/// <summary>
/// Phase 7.2.4 Shift Engine (Specialist Schedule) Backend Integration -
/// same "wrap the real service with permission enforcement" pattern as
/// <c>Specialists.SpecialistCommandServicePermissionGate</c>. Gates on
/// either <c>MANAGE_SCHEDULE_ALL</c> (owner/manager, any specialist) or
/// <c>MANAGE_SCHEDULE_OWN</c> (a specialist managing their own schedule) -
/// both confirmed existing, real backend permissions
/// (<c>ai.rojan.backend.domain.salon.Permission</c>), enforced again
/// server-side by ROJAN_Backend's own schedule use cases regardless of
/// what this gate decides - this is a fail-fast client-side check, not the
/// real authority. No Desktop-side permission enum change was needed to
/// support this: <see cref="IEnterpriseContext.BackendPermissions"/> is a
/// raw, backend-sourced string set already, per that interface's own doc
/// comment.
/// </summary>
public sealed class SpecialistScheduleCommandServicePermissionGate : ISpecialistScheduleCommandService
{
    private const string ManageScheduleAll = "MANAGE_SCHEDULE_ALL";
    private const string ManageScheduleOwn = "MANAGE_SCHEDULE_OWN";

    private readonly ISpecialistScheduleCommandService _inner;
    private readonly IBackendPermissionGate _backendPermissionGate;

    public SpecialistScheduleCommandServicePermissionGate(ISpecialistScheduleCommandService inner, IBackendPermissionGate backendPermissionGate)
    {
        _inner = inner;
        _backendPermissionGate = backendPermissionGate;
    }

    public Task<WeeklyAvailabilityDto> SetWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, IReadOnlyList<TimeIntervalDto> intervals, CancellationToken cancellationToken = default)
    {
        EnsurePermission();
        return _inner.SetWeeklyAvailabilityAsync(specialistId, dayOfWeek, intervals, cancellationToken);
    }

    public Task RemoveWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default)
    {
        EnsurePermission();
        return _inner.RemoveWeeklyAvailabilityAsync(specialistId, dayOfWeek, cancellationToken);
    }

    public Task<ScheduleOverrideDto> SetOverrideAsync(string specialistId, DateOnly scheduleDate, IReadOnlyList<TimeIntervalDto> intervals, string? reason, CancellationToken cancellationToken = default)
    {
        EnsurePermission();
        return _inner.SetOverrideAsync(specialistId, scheduleDate, intervals, reason, cancellationToken);
    }

    public Task RemoveOverrideAsync(string specialistId, string overrideId, CancellationToken cancellationToken = default)
    {
        EnsurePermission();
        return _inner.RemoveOverrideAsync(specialistId, overrideId, cancellationToken);
    }

    public Task<SpecialistLeaveDto> CreateLeaveAsync(string specialistId, DateOnly startDate, DateOnly endDate, string? reason, CancellationToken cancellationToken = default)
    {
        EnsurePermission();
        return _inner.CreateLeaveAsync(specialistId, startDate, endDate, reason, cancellationToken);
    }

    public Task RemoveLeaveAsync(string specialistId, string leaveId, CancellationToken cancellationToken = default)
    {
        EnsurePermission();
        return _inner.RemoveLeaveAsync(specialistId, leaveId, cancellationToken);
    }

    public Task<SpecialistBlockDto> CreateBlockAsync(string specialistId, DateOnly scheduleDate, TimeIntervalDto interval, string? reason, CancellationToken cancellationToken = default)
    {
        EnsurePermission();
        return _inner.CreateBlockAsync(specialistId, scheduleDate, interval, reason, cancellationToken);
    }

    public Task RemoveBlockAsync(string specialistId, string blockId, CancellationToken cancellationToken = default)
    {
        EnsurePermission();
        return _inner.RemoveBlockAsync(specialistId, blockId, cancellationToken);
    }

    private void EnsurePermission() => _backendPermissionGate.EnsureBackendAny(ManageScheduleAll, ManageScheduleOwn);
}
