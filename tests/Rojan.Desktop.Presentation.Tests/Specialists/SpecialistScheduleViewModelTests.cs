using Rojan.Desktop.Application.Schedule;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Specialists;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

/// <summary>
/// Exercises <see cref="SpecialistScheduleViewModel"/> - real Backend-authoritative data
/// only, this ViewModel never computes a value itself. Covers the three areas this
/// phase's own test requirements name: schedule loading, availability rendering, and
/// Backend failure handling.
/// </summary>
public sealed class SpecialistScheduleViewModelTests
{
    [Fact]
    public async Task Constructor_QueryStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<WeeklyAvailabilityDto>>();
        var queryService = new StubQueryService { WeeklyAvailabilityFactory = _ => tcs.Task };

        var sut = new SpecialistScheduleViewModel("specialist-1", queryService, new StubCommandService());

        Assert.Equal(DashboardState.Loading, sut.State);
        tcs.SetResult([]);
        await Task.Yield();
    }

    [Fact]
    public void Constructor_RealWeeklyAvailabilityReturned_PopulatesMatchingDayRowAndLeavesOthersClosed()
    {
        var monday = new WeeklyAvailabilityDto("avail-1", "specialist-1", DayOfWeek.Monday, [new TimeIntervalDto(new TimeOnly(9, 0), new TimeOnly(17, 0))], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var queryService = new StubQueryService { WeeklyAvailabilityFactory = _ => Task.FromResult<IReadOnlyList<WeeklyAvailabilityDto>>([monday]) };

        var sut = new SpecialistScheduleViewModel("specialist-1", queryService, new StubCommandService());

        Assert.Equal(DashboardState.Loaded, sut.State);
        var mondayRow = sut.WeeklyAvailability.Single(row => row.DayOfWeek == DayOfWeek.Monday);
        Assert.NotNull(mondayRow.Availability);
        var tuesdayRow = sut.WeeklyAvailability.Single(row => row.DayOfWeek == DayOfWeek.Tuesday);
        Assert.Null(tuesdayRow.Availability);
    }

    [Fact]
    public void Constructor_QueryThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubQueryService { WeeklyAvailabilityFactory = _ => Task.FromException<IReadOnlyList<WeeklyAvailabilityDto>>(new InvalidOperationException("boom")) };

        var sut = new SpecialistScheduleViewModel("specialist-1", queryService, new StubCommandService());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void BeginEditDayCommand_MarksOnlyThatRowAsEditing()
    {
        var queryService = new StubQueryService();
        var sut = new SpecialistScheduleViewModel("specialist-1", queryService, new StubCommandService());
        var mondayRow = sut.WeeklyAvailability.Single(row => row.DayOfWeek == DayOfWeek.Monday);
        var tuesdayRow = sut.WeeklyAvailability.Single(row => row.DayOfWeek == DayOfWeek.Tuesday);

        sut.BeginEditDayCommand.Execute(mondayRow);

        Assert.True(mondayRow.IsEditing);
        Assert.False(tuesdayRow.IsEditing);
    }

    [Fact]
    public void SaveDayAvailabilityCommand_CallsCommandServiceWithParsedInterval()
    {
        var commandService = new StubCommandService();
        var sut = new SpecialistScheduleViewModel("specialist-1", new StubQueryService(), commandService);
        var mondayRow = sut.WeeklyAvailability.Single(row => row.DayOfWeek == DayOfWeek.Monday);
        sut.BeginEditDayCommand.Execute(mondayRow);
        sut.EditIntervalStart = "09:00";
        sut.EditIntervalEnd = "17:00";

        sut.SaveDayAvailabilityCommand.Execute(null);

        var call = Assert.Single(commandService.SetWeeklyAvailabilityCalls);
        Assert.Equal(DayOfWeek.Monday, call.DayOfWeek);
        Assert.Equal(new TimeOnly(9, 0), call.Intervals[0].Start);
        Assert.Equal(new TimeOnly(17, 0), call.Intervals[0].End);
    }

    private sealed class StubQueryService : IScheduleQueryService
    {
        public Func<string, Task<IReadOnlyList<WeeklyAvailabilityDto>>> WeeklyAvailabilityFactory { get; set; } = _ => Task.FromResult<IReadOnlyList<WeeklyAvailabilityDto>>([]);

        public Task<IReadOnlyList<WeeklyAvailabilityDto>> GetWeeklyAvailabilityAsync(string specialistId, CancellationToken cancellationToken = default) =>
            WeeklyAvailabilityFactory(specialistId);

        public Task<IReadOnlyList<ScheduleOverrideDto>> GetOverridesAsync(string specialistId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ScheduleOverrideDto>>([]);

        public Task<IReadOnlyList<SpecialistLeaveDto>> GetLeavesAsync(string specialistId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SpecialistLeaveDto>>([]);

        public Task<IReadOnlyList<SpecialistBlockDto>> GetBlocksAsync(string specialistId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SpecialistBlockDto>>([]);
    }

    private sealed class StubCommandService : IScheduleCommandService
    {
        public List<(string SpecialistId, DayOfWeek DayOfWeek, IReadOnlyList<TimeIntervalDto> Intervals)> SetWeeklyAvailabilityCalls { get; } = [];

        public Task<WeeklyAvailabilityDto> SetWeeklyAvailabilityAsync(string specialistId, DayOfWeek dayOfWeek, IReadOnlyList<TimeIntervalDto> intervals, CancellationToken cancellationToken = default)
        {
            SetWeeklyAvailabilityCalls.Add((specialistId, dayOfWeek, intervals));
            return Task.FromResult(new WeeklyAvailabilityDto("avail-1", specialistId, dayOfWeek, intervals, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        }

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
}
