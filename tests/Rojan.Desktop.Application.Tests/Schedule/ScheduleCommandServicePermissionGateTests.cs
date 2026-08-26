using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Schedule;

namespace Rojan.Desktop.Application.Tests.Schedule;

/// <summary>
/// Exercises <see cref="ScheduleCommandServicePermissionGate"/> - real backend
/// <c>MANAGE_SCHEDULE_ALL</c> permission is this decorator's sole authority, same
/// "wrap with IBackendPermissionGate" pattern <c>BookingCommandServicePermissionGateTests</c>
/// already established. A caller granted only <c>MANAGE_SCHEDULE_OWN</c> (the real backend's
/// specialist-managing-their-own-record case, per <c>SalonPermissionResolver.canManageSpecialist</c>)
/// is correctly, uniformly denied here - deliberately excluded, not a mismatch (see this
/// gate's own doc comment).
/// </summary>
public sealed class ScheduleCommandServicePermissionGateTests
{
    private static ScheduleCommandServicePermissionGate CreateSut(IReadOnlySet<string> backendPermissions) =>
        new(new StubScheduleCommandService(), new BackendPermissionGate(new StubEnterpriseContext { BackendPermissions = backendPermissions }));

    [Fact]
    public async Task SetWeeklyAvailabilityAsync_ManageScheduleAll_Allowed()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_SCHEDULE_ALL" });

        var exception = await Record.ExceptionAsync(() => sut.SetWeeklyAvailabilityAsync("specialist-1", DayOfWeek.Monday, [new TimeIntervalDto(new TimeOnly(9, 0), new TimeOnly(17, 0))]));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SetWeeklyAvailabilityAsync_NoPermission_Denied()
    {
        var sut = CreateSut(new HashSet<string>());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            sut.SetWeeklyAvailabilityAsync("specialist-1", DayOfWeek.Monday, [new TimeIntervalDto(new TimeOnly(9, 0), new TimeOnly(17, 0))]));
    }

    [Fact]
    public async Task SetWeeklyAvailabilityAsync_OnlyManageScheduleOwn_Denied()
    {
        // Real backend grants MANAGE_SCHEDULE_OWN to a specialist's own link (SalonPermissionResolver.resolve),
        // deliberately excluded from this check - see this gate's own doc comment.
        var sut = CreateSut(new HashSet<string> { "MANAGE_SCHEDULE_OWN" });

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            sut.SetWeeklyAvailabilityAsync("specialist-1", DayOfWeek.Monday, [new TimeIntervalDto(new TimeOnly(9, 0), new TimeOnly(17, 0))]));
    }

    [Fact]
    public async Task CreateLeaveAsync_ManageScheduleAll_Allowed()
    {
        var sut = CreateSut(new HashSet<string> { "MANAGE_SCHEDULE_ALL" });

        var exception = await Record.ExceptionAsync(() => sut.CreateLeaveAsync("specialist-1", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 5), "Vacation"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task CreateLeaveAsync_NoPermission_Denied()
    {
        var sut = CreateSut(new HashSet<string>());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            sut.CreateLeaveAsync("specialist-1", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 5), "Vacation"));
    }

    [Fact]
    public async Task CreateBlockAsync_NoPermission_Denied()
    {
        var sut = CreateSut(new HashSet<string>());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() =>
            sut.CreateBlockAsync("specialist-1", new DateOnly(2026, 6, 1), new TimeOnly(13, 0), new TimeOnly(14, 0), "Dentist"));
    }

    [Fact]
    public async Task RemoveWeeklyAvailabilityAsync_NoPermission_Denied()
    {
        var sut = CreateSut(new HashSet<string>());

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.RemoveWeeklyAvailabilityAsync("specialist-1", DayOfWeek.Monday));
    }

    private sealed class StubScheduleCommandService : IScheduleCommandService
    {
        public Task<WeeklyAvailabilityDto> SetWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, IReadOnlyList<TimeIntervalDto> intervals, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WeeklyAvailabilityDto("avail-1", specialistId, dayOfWeek, intervals, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        public Task RemoveWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<ScheduleOverrideDto> SetOverrideAsync(string specialistId, DateOnly scheduleDate, IReadOnlyList<TimeIntervalDto> intervals, string? reason, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ScheduleOverrideDto("override-1", specialistId, scheduleDate, intervals, reason, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        public Task RemoveOverrideAsync(string specialistId, string overrideId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<SpecialistLeaveDto> CreateLeaveAsync(string specialistId, DateOnly startDate, DateOnly endDate, string? reason, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SpecialistLeaveDto("leave-1", specialistId, startDate, endDate, reason, DateTimeOffset.UtcNow));

        public Task RemoveLeaveAsync(string specialistId, string leaveId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<SpecialistBlockDto> CreateBlockAsync(string specialistId, DateOnly scheduleDate, TimeOnly start, TimeOnly endTime, string? reason, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SpecialistBlockDto("block-1", specialistId, scheduleDate, start, endTime, reason, DateTimeOffset.UtcNow));

        public Task RemoveBlockAsync(string specialistId, string blockId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubEnterpriseContext : IEnterpriseContext
    {
        public string? CurrentOrganizationId => "org-1";

        public string? CurrentBranchId => "branch-1";

        public WorkspaceRole CurrentRole => WorkspaceRole.PlatformOwner;

        public IReadOnlySet<string> BackendPermissions { get; set; } = new HashSet<string>();
    }
}
