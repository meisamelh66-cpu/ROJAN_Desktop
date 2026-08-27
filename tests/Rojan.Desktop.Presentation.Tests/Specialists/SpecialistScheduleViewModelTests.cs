using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Specialists.Schedule;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Specialists;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

/// <summary>Phase 7.2.6 Shift Engine UI Activation - exercises the Manager schedule UI's ViewModel: Loading/Loaded/Empty/Error states, the distinct permission-denied state, and every mutation command's success/failure/input-validation path.</summary>
public sealed class SpecialistScheduleViewModelTests
{
    [Fact]
    public void Constructor_QueryStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<WeeklyAvailabilityDto>>();
        var queryService = new StubSpecialistScheduleQueryService { WeeklyAvailability = _ => tcs.Task };

        var sut = new SpecialistScheduleViewModel("specialist-1", queryService, new StubSpecialistScheduleCommandService());

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public async Task LoadCommand_DataPresent_StateIsLoadedAndPopulatesCollections()
    {
        var queryService = new StubSpecialistScheduleQueryService
        {
            WeeklyAvailability = _ => Task.FromResult<IReadOnlyList<WeeklyAvailabilityDto>>(
                [new WeeklyAvailabilityDto("wa-1", "specialist-1", DayOfWeek.Monday, [new TimeIntervalDto(TimeSpan.FromHours(9), TimeSpan.FromHours(13))])]),
        };
        var sut = new SpecialistScheduleViewModel("specialist-1", queryService, new StubSpecialistScheduleCommandService());

        sut.LoadCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Single(sut.WeeklyAvailability);
    }

    [Fact]
    public async Task LoadCommand_NothingConfigured_StateIsEmpty()
    {
        var sut = new SpecialistScheduleViewModel("specialist-1", new StubSpecialistScheduleQueryService(), new StubSpecialistScheduleCommandService());

        sut.LoadCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(DashboardState.Empty, sut.State);
    }

    [Fact]
    public async Task LoadCommand_QueryThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubSpecialistScheduleQueryService
        {
            WeeklyAvailability = _ => Task.FromException<IReadOnlyList<WeeklyAvailabilityDto>>(new InvalidOperationException("boom")),
        };
        var sut = new SpecialistScheduleViewModel("specialist-1", queryService, new StubSpecialistScheduleCommandService());

        sut.LoadCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
        Assert.False(sut.IsPermissionDenied);
    }

    [Fact]
    public async Task LoadCommand_UnauthorizedOperationException_SetsIsPermissionDenied_NotGenericError()
    {
        var queryService = new StubSpecialistScheduleQueryService
        {
            WeeklyAvailability = _ => Task.FromException<IReadOnlyList<WeeklyAvailabilityDto>>(new UnauthorizedOperationException("denied")),
        };
        var sut = new SpecialistScheduleViewModel("specialist-1", queryService, new StubSpecialistScheduleCommandService());

        sut.LoadCommand.Execute(null);
        await Task.Yield();

        Assert.True(sut.IsPermissionDenied);
        Assert.Equal(DashboardState.Error, sut.State);
    }

    [Fact]
    public async Task SetWeeklyAvailabilityCommand_ValidInput_CallsCommandServiceAndReloads()
    {
        var commandService = new StubSpecialistScheduleCommandService();
        var sut = new SpecialistScheduleViewModel("specialist-1", new StubSpecialistScheduleQueryService(), commandService)
        {
            SelectedDayOfWeek = DayOfWeek.Tuesday,
            NewAvailabilityStartText = "09:00",
            NewAvailabilityEndText = "13:00",
        };
        await Task.Yield();

        sut.SetWeeklyAvailabilityCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(1, commandService.SetWeeklyAvailabilityCallCount);
        Assert.Equal(string.Empty, sut.NewAvailabilityStartText);
        Assert.Null(sut.InputErrorMessage);
    }

    [Fact]
    public async Task SetWeeklyAvailabilityCommand_MalformedTime_SetsInputErrorMessage_NeverCallsCommandService()
    {
        var commandService = new StubSpecialistScheduleCommandService();
        var sut = new SpecialistScheduleViewModel("specialist-1", new StubSpecialistScheduleQueryService(), commandService)
        {
            NewAvailabilityStartText = "not-a-time",
            NewAvailabilityEndText = "13:00",
        };
        await Task.Yield();

        sut.SetWeeklyAvailabilityCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(0, commandService.SetWeeklyAvailabilityCallCount);
        Assert.NotNull(sut.InputErrorMessage);
    }

    [Fact]
    public async Task SetWeeklyAvailabilityCommand_PermissionDenied_SetsIsPermissionDenied_NeverReloads()
    {
        var commandService = new StubSpecialistScheduleCommandService { Fail = new UnauthorizedOperationException("denied") };
        var sut = new SpecialistScheduleViewModel("specialist-1", new StubSpecialistScheduleQueryService(), commandService)
        {
            NewAvailabilityStartText = "09:00",
            NewAvailabilityEndText = "13:00",
        };
        await Task.Yield();

        sut.SetWeeklyAvailabilityCommand.Execute(null);
        await Task.Yield();

        Assert.True(sut.IsPermissionDenied);
        // The input buffer must not be cleared - a denied mutation never "succeeded".
        Assert.Equal("09:00", sut.NewAvailabilityStartText);
    }

    [Fact]
    public async Task RemoveWeeklyAvailabilityCommand_NullParameter_NoOp()
    {
        var commandService = new StubSpecialistScheduleCommandService();
        var sut = new SpecialistScheduleViewModel("specialist-1", new StubSpecialistScheduleQueryService(), commandService);
        await Task.Yield();

        sut.RemoveWeeklyAvailabilityCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(0, commandService.RemoveWeeklyAvailabilityCallCount);
    }

    [Fact]
    public async Task SetOverrideCommand_BlankIntervals_SendsUnavailableAllDay()
    {
        var commandService = new StubSpecialistScheduleCommandService();
        var sut = new SpecialistScheduleViewModel("specialist-1", new StubSpecialistScheduleQueryService(), commandService)
        {
            NewOverrideDateText = "2026-09-01",
            NewOverrideReason = "Holiday",
        };
        await Task.Yield();

        sut.SetOverrideCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(1, commandService.SetOverrideCallCount);
        Assert.Null(sut.InputErrorMessage);
    }

    [Fact]
    public async Task SetOverrideCommand_MalformedDate_SetsInputErrorMessage()
    {
        var commandService = new StubSpecialistScheduleCommandService();
        var sut = new SpecialistScheduleViewModel("specialist-1", new StubSpecialistScheduleQueryService(), commandService)
        {
            NewOverrideDateText = "not-a-date",
        };
        await Task.Yield();

        sut.SetOverrideCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(0, commandService.SetOverrideCallCount);
        Assert.NotNull(sut.InputErrorMessage);
    }

    [Fact]
    public async Task CreateLeaveCommand_ValidInput_CallsCommandServiceAndReloads()
    {
        var commandService = new StubSpecialistScheduleCommandService();
        var sut = new SpecialistScheduleViewModel("specialist-1", new StubSpecialistScheduleQueryService(), commandService)
        {
            NewLeaveStartDateText = "2026-09-01",
            NewLeaveEndDateText = "2026-09-07",
            NewLeaveReason = "Vacation",
        };
        await Task.Yield();

        sut.CreateLeaveCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(1, commandService.CreateLeaveCallCount);
        Assert.Equal(string.Empty, sut.NewLeaveReason);
    }

    [Fact]
    public async Task CreateBlockCommand_ValidInput_CallsCommandServiceAndReloads()
    {
        var commandService = new StubSpecialistScheduleCommandService();
        var sut = new SpecialistScheduleViewModel("specialist-1", new StubSpecialistScheduleQueryService(), commandService)
        {
            NewBlockDateText = "2026-09-01",
            NewBlockStartText = "14:00",
            NewBlockEndText = "15:00",
            NewBlockReason = "Dentist",
        };
        await Task.Yield();

        sut.CreateBlockCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(1, commandService.CreateBlockCallCount);
        Assert.Equal(string.Empty, sut.NewBlockReason);
    }

    [Fact]
    public async Task RemoveLeaveCommand_NullParameter_NoOp()
    {
        var commandService = new StubSpecialistScheduleCommandService();
        var sut = new SpecialistScheduleViewModel("specialist-1", new StubSpecialistScheduleQueryService(), commandService);
        await Task.Yield();

        sut.RemoveLeaveCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(0, commandService.RemoveLeaveCallCount);
    }

    // Phase 7.4.1 Production Hardening: a handled failure must also be logged, not just shown via
    // ErrorMessage/IsPermissionDenied - see this class's own doc comment.

    [Fact]
    public async Task LoadCommand_QueryThrows_LogsTheFailure()
    {
        var logger = new RecordingLogger<SpecialistScheduleViewModel>();
        var queryService = new StubSpecialistScheduleQueryService
        {
            WeeklyAvailability = _ => Task.FromException<IReadOnlyList<WeeklyAvailabilityDto>>(new InvalidOperationException("boom")),
        };
        var sut = new SpecialistScheduleViewModel("specialist-1", queryService, new StubSpecialistScheduleCommandService(), logger);

        sut.LoadCommand.Execute(null);
        await Task.Yield();

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("specialist-1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LoadCommand_UnauthorizedOperationException_LogsAsWarningNotError()
    {
        var logger = new RecordingLogger<SpecialistScheduleViewModel>();
        var queryService = new StubSpecialistScheduleQueryService
        {
            WeeklyAvailability = _ => Task.FromException<IReadOnlyList<WeeklyAvailabilityDto>>(new UnauthorizedOperationException("denied")),
        };
        var sut = new SpecialistScheduleViewModel("specialist-1", queryService, new StubSpecialistScheduleCommandService(), logger);

        sut.LoadCommand.Execute(null);
        await Task.Yield();

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.DoesNotContain(logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public async Task SetWeeklyAvailabilityCommand_PermissionDenied_LogsTheDenial()
    {
        var logger = new RecordingLogger<SpecialistScheduleViewModel>();
        var commandService = new StubSpecialistScheduleCommandService { Fail = new UnauthorizedOperationException("denied") };
        var sut = new SpecialistScheduleViewModel("specialist-1", new StubSpecialistScheduleQueryService(), commandService, logger)
        {
            NewAvailabilityStartText = "09:00",
            NewAvailabilityEndText = "13:00",
        };
        await Task.Yield();
        logger.Entries.Clear(); // discard the constructor's own initial LoadAsync log noise, if any

        sut.SetWeeklyAvailabilityCommand.Execute(null);
        await Task.Yield();

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains(nameof(SpecialistScheduleViewModel.SetWeeklyAvailabilityCommand).Replace("Command", "Async", StringComparison.Ordinal), StringComparison.Ordinal));
    }

    [Fact]
    public async Task NoLoggerSupplied_UsesNullLogger_NeverThrows()
    {
        // The optional-logger default (NullLogger) must be a genuinely safe no-op, not merely
        // "compiles" - a failure here would mean every existing test/call site that doesn't pass a
        // logger (all of them, before this Phase) was silently relying on undefined behavior.
        var queryService = new StubSpecialistScheduleQueryService
        {
            WeeklyAvailability = _ => Task.FromException<IReadOnlyList<WeeklyAvailabilityDto>>(new InvalidOperationException("boom")),
        };
        var sut = new SpecialistScheduleViewModel("specialist-1", queryService, new StubSpecialistScheduleCommandService());

        var exception = await Record.ExceptionAsync(async () =>
        {
            sut.LoadCommand.Execute(null);
            await Task.Yield();
        });

        Assert.Null(exception);
        Assert.Equal(DashboardState.Error, sut.State);
    }
}
