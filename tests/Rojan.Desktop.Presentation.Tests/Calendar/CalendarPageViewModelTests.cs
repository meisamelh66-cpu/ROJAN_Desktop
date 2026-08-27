using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Calendar;
using Rojan.Desktop.Application.Services;
using Rojan.Desktop.Presentation.Tests.Services;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Calendar;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Calendar;

public sealed class CalendarPageViewModelTests
{
    private static ScheduledSpecialistDto MakeSpecialist(string id, string name) => new(id, name);

    private static ServiceDto MakeService(string id, string name, ServiceStatus status = ServiceStatus.Active) =>
        new(id, name, ServiceCategory.Hair, status, 30, "$0", string.Empty);

    private static AvailabilitySlotDto MakeSlot(string specialistId, AvailabilityStatus status) =>
        new(specialistId, "Jordan Lee", DateTimeOffset.Now, DateTimeOffset.Now.AddMinutes(30), status);

    private static DailyAvailabilityDto MakeAvailability(string specialistId, string specialistName, IReadOnlyList<AvailabilitySlotDto>? slots = null) =>
        new(specialistId, specialistName, DateOnly.FromDateTime(DateTime.Today), new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), slots ?? []);

    private static StubServiceQueryService MakeServiceQueryService(params ServiceDto[] services) =>
        new(_ => Task.FromResult<IReadOnlyList<ServiceDto>>(services));

    [Fact]
    public void Constructor_SpecialistsQueryStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<ScheduledSpecialistDto>>();
        var queryService = new StubCalendarQueryService(
            _ => tcs.Task,
            (specialistId, _, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee")));

        var sut = new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-1", "Haircut")));

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_NoScheduledSpecialists_StateIsEmpty()
    {
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>([]),
            (specialistId, _, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee")));

        var sut = new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-1", "Haircut")));

        Assert.Equal(DashboardState.Empty, sut.State);
        Assert.Null(sut.SelectedSpecialist);
    }

    [Fact]
    public void Constructor_NoActiveServices_StateIsEmpty()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (specialistId, _, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee")));

        var sut = new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-9", "Retired", ServiceStatus.Discontinued)));

        Assert.Equal(DashboardState.Empty, sut.State);
        Assert.Null(sut.SelectedService);
    }

    [Fact]
    public void Constructor_SpecialistsAndServicesAvailable_SelectsFirstOfEachAndLoadsAvailability()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var slot = MakeSlot("specialist-1", AvailabilityStatus.Available);
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (specialistId, _, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee", [slot])));
        var service = MakeService("service-1", "Haircut");

        var sut = new CalendarPageViewModel(queryService, MakeServiceQueryService(service));

        Assert.Equal(specialists[0], sut.SelectedSpecialist);
        Assert.Equal(service, sut.SelectedService);
        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Single(sut.Slots);
        Assert.StartsWith("Working", sut.WorkingHoursText, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_SpecialistsQueryThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubCalendarQueryService(
            _ => Task.FromException<IReadOnlyList<ScheduledSpecialistDto>>(new InvalidOperationException("boom")),
            (specialistId, _, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee")));

        var sut = new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-1", "Haircut")));

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    // Phase 8.11 Logging Hardening: the three broad-catch load boundaries
    // (InitializeAsync / LoadDailyAvailabilityAsync / LoadWeeklyAvailabilityAsync)
    // now log at Error before surfacing the Error state - user-visible behaviour
    // (ErrorMessage / State) is unchanged, verified by the existing tests above.

    [Fact]
    public void InitializeAsync_SpecialistsQueryThrows_LogsErrorWithOperation()
    {
        var queryService = new StubCalendarQueryService(
            _ => Task.FromException<IReadOnlyList<ScheduledSpecialistDto>>(new InvalidOperationException("boom")),
            (specialistId, _, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee")));
        var logger = new RecordingLogger<CalendarPageViewModel>();

        var sut = new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-1", "Haircut")), logger);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("InitializeAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void LoadDailyAvailabilityAsync_Throws_LogsErrorWithOperation()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (_, _, _, _) => Task.FromException<DailyAvailabilityDto>(new InvalidOperationException("boom")));
        var logger = new RecordingLogger<CalendarPageViewModel>();

        var sut = new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-1", "Haircut")), logger);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("LoadDailyAvailabilityAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void LoadWeeklyAvailabilityAsync_Throws_LogsErrorWithOperation()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var slot = MakeSlot("specialist-1", AvailabilityStatus.Available);
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (specialistId, _, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee", [slot])),
            (_, _, _, _) => Task.FromException<WeeklyAvailabilityDto>(new InvalidOperationException("boom")));
        var logger = new RecordingLogger<CalendarPageViewModel>();
        var sut = new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-1", "Haircut")), logger);
        Assert.Equal(DashboardState.Loaded, sut.State);

        sut.SetViewModeCommand.Execute(CalendarViewMode.Week);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("LoadWeeklyAvailabilityAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void NoLoggerSupplied_UsesNullLogger_InitializeFailureNeverThrows()
    {
        var queryService = new StubCalendarQueryService(
            _ => Task.FromException<IReadOnlyList<ScheduledSpecialistDto>>(new InvalidOperationException("boom")),
            (specialistId, _, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee")));

        var exception = Record.Exception(() =>
            new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-1", "Haircut"))));

        Assert.Null(exception);
    }

    [Fact]
    public void SelectedDate_Changed_ReloadsAvailability()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var callCount = 0;
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (specialistId, _, _, _) =>
            {
                callCount++;
                return Task.FromResult(MakeAvailability(specialistId, "Jordan Lee"));
            });
        var sut = new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-1", "Haircut")));
        var countAfterConstruction = callCount;

        sut.SelectedDate = DateTime.Today.AddDays(5);

        Assert.True(callCount > countAfterConstruction);
    }

    [Fact]
    public void SelectedService_Changed_ReloadsAvailability()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var callCount = 0;
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (specialistId, _, _, _) =>
            {
                callCount++;
                return Task.FromResult(MakeAvailability(specialistId, "Jordan Lee"));
            });
        var sut = new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-1", "Haircut"), MakeService("service-2", "Colour")));
        var countAfterConstruction = callCount;

        sut.SelectedService = new ServiceDto("service-2", "Colour", ServiceCategory.Hair, ServiceStatus.Active, 45, "$0", string.Empty);

        Assert.True(callCount > countAfterConstruction);
    }

    [Fact]
    public void GetDailyAvailabilityAsync_CalledWithSelectedSpecialistAndServiceIds()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        (string SpecialistId, string ServiceId)? lastCall = null;
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (specialistId, serviceId, _, _) =>
            {
                lastCall = (specialistId, serviceId);
                return Task.FromResult(MakeAvailability(specialistId, "Jordan Lee"));
            });

        _ = new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-1", "Haircut")));

        Assert.Equal(("specialist-1", "service-1"), lastCall);
    }

    [Fact]
    public void LoadCommand_ExecutedAfterFailure_RecoversToLoadedState()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var shouldFail = true;
        var slot = MakeSlot("specialist-1", AvailabilityStatus.Available);
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (specialistId, _, _, _) => shouldFail
                ? Task.FromException<DailyAvailabilityDto>(new InvalidOperationException("boom"))
                : Task.FromResult(MakeAvailability(specialistId, "Jordan Lee", [slot])));
        var sut = new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-1", "Haircut")));
        Assert.Equal(DashboardState.Error, sut.State);

        shouldFail = false;
        sut.LoadCommand.Execute(null);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Null(sut.ErrorMessage);
    }

    [Fact]
    public void Constructor_DefaultViewMode_IsDay()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (specialistId, _, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee")));

        var sut = new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-1", "Haircut")));

        Assert.Equal(CalendarViewMode.Day, sut.ViewMode);
        Assert.True(sut.IsDayView);
        Assert.False(sut.IsWeekView);
    }

    [Fact]
    public void SetViewModeCommand_Week_SwitchesViewModeAndLoadsWeekDays()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var slot = MakeSlot("specialist-1", AvailabilityStatus.Available);
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (specialistId, _, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee", [slot])));
        var sut = new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-1", "Haircut")));

        sut.SetViewModeCommand.Execute(CalendarViewMode.Week);

        Assert.Equal(CalendarViewMode.Week, sut.ViewMode);
        Assert.True(sut.IsWeekView);
        Assert.False(sut.IsDayView);
        Assert.Equal(7, sut.WeekDays.Count);
        Assert.Empty(sut.Slots);
        Assert.Equal(DashboardState.Loaded, sut.State);
    }

    [Fact]
    public void SetViewModeCommand_BackToDay_RepopulatesSlotsAndClearsWeekDays()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var slot = MakeSlot("specialist-1", AvailabilityStatus.Available);
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (specialistId, _, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee", [slot])));
        var sut = new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-1", "Haircut")));
        sut.SetViewModeCommand.Execute(CalendarViewMode.Week);

        sut.SetViewModeCommand.Execute(CalendarViewMode.Day);

        Assert.Equal(CalendarViewMode.Day, sut.ViewMode);
        Assert.Empty(sut.WeekDays);
        Assert.Single(sut.Slots);
        Assert.Equal(DashboardState.Loaded, sut.State);
    }

    [Fact]
    public void SetViewModeCommand_NoScheduledSpecialists_Week_StateIsEmpty()
    {
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>([]),
            (specialistId, _, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee")));
        var sut = new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-1", "Haircut")));

        sut.SetViewModeCommand.Execute(CalendarViewMode.Week);

        Assert.Equal(DashboardState.Empty, sut.State);
    }

    [Fact]
    public void SetViewModeCommand_ToggledRepeatedly_EndsInConsistentState()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var slot = MakeSlot("specialist-1", AvailabilityStatus.Available);
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (specialistId, _, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee", [slot])));
        var sut = new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-1", "Haircut")));

        sut.SetViewModeCommand.Execute(CalendarViewMode.Week);
        sut.SetViewModeCommand.Execute(CalendarViewMode.Day);
        sut.SetViewModeCommand.Execute(CalendarViewMode.Week);
        sut.SetViewModeCommand.Execute(CalendarViewMode.Day);

        Assert.Equal(CalendarViewMode.Day, sut.ViewMode);
        Assert.True(sut.IsDayView);
        Assert.False(sut.IsWeekView);
        Assert.Single(sut.Slots);
        Assert.Empty(sut.WeekDays);
        Assert.Equal(DashboardState.Loaded, sut.State);
    }

    [Fact]
    public void SetViewModeCommand_SameModeExecutedTwice_DoesNotReload()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var callCount = 0;
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (specialistId, _, _, _) =>
            {
                callCount++;
                return Task.FromResult(MakeAvailability(specialistId, "Jordan Lee"));
            });
        var sut = new CalendarPageViewModel(queryService, MakeServiceQueryService(MakeService("service-1", "Haircut")));
        var countAfterConstruction = callCount;

        sut.SetViewModeCommand.Execute(CalendarViewMode.Day);

        Assert.Equal(countAfterConstruction, callCount);
    }
}
