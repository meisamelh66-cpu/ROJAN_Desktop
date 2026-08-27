using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Specialists.Schedule;
using Rojan.Desktop.Application.Tests.Organizations;

namespace Rojan.Desktop.Application.Tests.Specialists.Schedule;

/// <summary>
/// Exercises <see cref="SpecialistScheduleCommandServicePermissionGate"/> -
/// both real, existing backend permissions
/// (<c>MANAGE_SCHEDULE_ALL</c>/<c>MANAGE_SCHEDULE_OWN</c>) allow every
/// method, and a session with neither is denied. Same shape as
/// <c>Specialists.SpecialistCommandServicePermissionGateTests</c>.
/// </summary>
public sealed class SpecialistScheduleCommandServicePermissionGateTests
{
    private static SpecialistScheduleCommandServicePermissionGate CreateSut(IReadOnlySet<string> backendPermissions) =>
        new(new StubSpecialistScheduleRepositoryCommandService(), new BackendPermissionGate(new StubEnterpriseContext { BackendPermissions = backendPermissions }));

    [Fact]
    public async Task SetWeeklyAvailabilityAsync_ManageScheduleAll_Allowed()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_SCHEDULE_ALL" });

        var exception = await Record.ExceptionAsync(() => sut.SetWeeklyAvailabilityAsync("specialist-1", DayOfWeek.Monday, []));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SetWeeklyAvailabilityAsync_ManageScheduleOwn_Allowed()
    {
        // Unlike SpecialistCommandServicePermissionGate (which deliberately checks only
        // MANAGE_STAFF), this gate allows MANAGE_SCHEDULE_OWN too - a specialist managing their
        // own availability window is exactly the Official Shift definition's intended actor.
        var sut = CreateSut(new HashSet<string> { "MANAGE_SCHEDULE_OWN" });

        var exception = await Record.ExceptionAsync(() => sut.SetWeeklyAvailabilityAsync("specialist-1", DayOfWeek.Monday, []));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SetWeeklyAvailabilityAsync_NoBackendPermissions_Denied()
    {
        var sut = CreateSut(new HashSet<string>());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.SetWeeklyAvailabilityAsync("specialist-1", DayOfWeek.Monday, []));
    }

    [Fact]
    public async Task SetWeeklyAvailabilityAsync_UnrelatedPermission_Denied()
    {
        // The real backend RECEPTIONIST role never has either schedule permission.
        var sut = CreateSut(new HashSet<string> { "MANAGE_BOOKINGS" });

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.SetWeeklyAvailabilityAsync("specialist-1", DayOfWeek.Monday, []));
    }

    [Fact]
    public async Task CreateLeaveAsync_ManageScheduleAll_Allowed()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_SCHEDULE_ALL" });

        var exception = await Record.ExceptionAsync(() => sut.CreateLeaveAsync("specialist-1", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 7), "Vacation"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task CreateLeaveAsync_NoBackendPermissions_Denied()
    {
        var sut = CreateSut(new HashSet<string>());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateLeaveAsync("specialist-1", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 7), "Vacation"));
    }

    [Fact]
    public async Task CreateBlockAsync_NoBackendPermissions_Denied()
    {
        var sut = CreateSut(new HashSet<string>());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            sut.CreateBlockAsync("specialist-1", new DateOnly(2026, 9, 1), new TimeIntervalDto(TimeSpan.Zero, TimeSpan.FromHours(1)), null));
    }

    [Fact]
    public async Task RemoveOverrideAsync_NoBackendPermissions_Denied()
    {
        var sut = CreateSut(new HashSet<string>());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.RemoveOverrideAsync("specialist-1", "ov-1"));
    }

    private sealed class StubSpecialistScheduleRepositoryCommandService : ISpecialistScheduleCommandService
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
}
