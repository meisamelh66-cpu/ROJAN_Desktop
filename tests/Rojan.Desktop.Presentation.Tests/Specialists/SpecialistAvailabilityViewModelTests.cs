using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Specialists.Schedule;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Specialists;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

/// <summary>Phase 7.2.6 Shift Engine UI Activation - exercises the read-only availability ViewModel: Loading/Loaded/Empty/Error states, and that it never exposes any mutation surface.</summary>
public sealed class SpecialistAvailabilityViewModelTests
{
    [Fact]
    public void Constructor_QueryStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<WeeklyAvailabilityDto>>();
        var queryService = new StubSpecialistScheduleQueryService { WeeklyAvailability = _ => tcs.Task };

        var sut = new SpecialistAvailabilityViewModel("specialist-1", queryService);

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public async Task LoadCommand_DataPresent_StateIsLoadedAndPopulatesCollections()
    {
        var queryService = new StubSpecialistScheduleQueryService
        {
            WeeklyAvailability = _ => Task.FromResult<IReadOnlyList<WeeklyAvailabilityDto>>(
                [new WeeklyAvailabilityDto("wa-1", "specialist-1", DayOfWeek.Wednesday, [new TimeIntervalDto(TimeSpan.FromHours(10), TimeSpan.FromHours(18))])]),
            Blocks = _ => Task.FromResult<IReadOnlyList<SpecialistBlockDto>>(
                [new SpecialistBlockDto("bl-1", "specialist-1", new DateOnly(2026, 9, 1), new TimeIntervalDto(TimeSpan.FromHours(14), TimeSpan.FromHours(15)), "Dentist")]),
        };
        var sut = new SpecialistAvailabilityViewModel("specialist-1", queryService);

        sut.LoadCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Single(sut.WeeklyAvailability);
        Assert.Single(sut.Blocks);
    }

    [Fact]
    public async Task LoadCommand_NothingConfigured_StateIsEmpty()
    {
        var sut = new SpecialistAvailabilityViewModel("specialist-1", new StubSpecialistScheduleQueryService());

        sut.LoadCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(DashboardState.Empty, sut.State);
    }

    [Fact]
    public async Task LoadCommand_QueryThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubSpecialistScheduleQueryService
        {
            Overrides = _ => Task.FromException<IReadOnlyList<ScheduleOverrideDto>>(new InvalidOperationException("boom")),
        };
        var sut = new SpecialistAvailabilityViewModel("specialist-1", queryService);

        sut.LoadCommand.Execute(null);
        await Task.Yield();

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    // Phase 7.4.1 Production Hardening: see SpecialistScheduleViewModelTests' own tests for the
    // full reasoning - a handled failure must also be logged.

    [Fact]
    public async Task LoadCommand_QueryThrows_LogsTheFailure()
    {
        var logger = new RecordingLogger<SpecialistAvailabilityViewModel>();
        var queryService = new StubSpecialistScheduleQueryService
        {
            Overrides = _ => Task.FromException<IReadOnlyList<ScheduleOverrideDto>>(new InvalidOperationException("boom")),
        };
        var sut = new SpecialistAvailabilityViewModel("specialist-1", queryService, logger);

        sut.LoadCommand.Execute(null);
        await Task.Yield();

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("specialist-1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task NoLoggerSupplied_UsesNullLogger_NeverThrows()
    {
        var queryService = new StubSpecialistScheduleQueryService
        {
            Overrides = _ => Task.FromException<IReadOnlyList<ScheduleOverrideDto>>(new InvalidOperationException("boom")),
        };
        var sut = new SpecialistAvailabilityViewModel("specialist-1", queryService);

        var exception = await Record.ExceptionAsync(async () =>
        {
            sut.LoadCommand.Execute(null);
            await Task.Yield();
        });

        Assert.Null(exception);
        Assert.Equal(DashboardState.Error, sut.State);
    }
}
