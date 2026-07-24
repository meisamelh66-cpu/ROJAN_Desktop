using Rojan.Desktop.Application.Calendar;
using Rojan.Desktop.Presentation.ViewModels.Calendar;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Calendar;

public sealed class CalendarPageViewModelTests
{
    private static ScheduledSpecialistDto MakeSpecialist(string id, string name) => new(id, name);

    private static AvailabilitySlotDto MakeSlot(string specialistId, AvailabilityStatus status) =>
        new(specialistId, "Jordan Lee", DateTimeOffset.Now, DateTimeOffset.Now.AddMinutes(30), status);

    private static DailyAvailabilityDto MakeAvailability(string specialistId, string specialistName, IReadOnlyList<AvailabilitySlotDto>? slots = null) =>
        new(specialistId, specialistName, DateOnly.FromDateTime(DateTime.Today), new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), slots ?? []);

    [Fact]
    public void Constructor_SpecialistsQueryStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<ScheduledSpecialistDto>>();
        var queryService = new StubCalendarQueryService(
            _ => tcs.Task,
            (specialistId, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee")));

        var sut = new CalendarPageViewModel(queryService, new StubCalendarCommandService());

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_NoScheduledSpecialists_StateIsEmpty()
    {
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>([]),
            (specialistId, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee")));

        var sut = new CalendarPageViewModel(queryService, new StubCalendarCommandService());

        Assert.Equal(DashboardState.Empty, sut.State);
        Assert.Null(sut.SelectedSpecialist);
    }

    [Fact]
    public void Constructor_SpecialistsAvailable_SelectsFirstAndLoadsAvailability()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var slot = MakeSlot("specialist-1", AvailabilityStatus.Available);
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (specialistId, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee", [slot])));

        var sut = new CalendarPageViewModel(queryService, new StubCalendarCommandService());

        Assert.Equal(specialists[0], sut.SelectedSpecialist);
        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Single(sut.Slots);
        Assert.StartsWith("Working", sut.WorkingHoursText, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_SpecialistsQueryThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubCalendarQueryService(
            _ => Task.FromException<IReadOnlyList<ScheduledSpecialistDto>>(new InvalidOperationException("boom")),
            (specialistId, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee")));

        var sut = new CalendarPageViewModel(queryService, new StubCalendarCommandService());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void SelectedDate_Changed_ReloadsAvailability()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var callCount = 0;
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (specialistId, _, _) =>
            {
                callCount++;
                return Task.FromResult(MakeAvailability(specialistId, "Jordan Lee"));
            });
        var sut = new CalendarPageViewModel(queryService, new StubCalendarCommandService());
        var countAfterConstruction = callCount;

        sut.SelectedDate = DateTime.Today.AddDays(5);

        Assert.True(callCount > countAfterConstruction);
    }

    [Fact]
    public void ToggleSlotCommand_AvailableSlot_CallsReserveSlotAsync()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (specialistId, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee")));
        var commandService = new StubCalendarCommandService();
        var sut = new CalendarPageViewModel(queryService, commandService);
        var slot = MakeSlot("specialist-1", AvailabilityStatus.Available);

        sut.ToggleSlotCommand.Execute(slot);

        var call = Assert.Single(commandService.ReserveCalls);
        Assert.Equal("specialist-1", call.SpecialistId);
        Assert.Empty(commandService.ReleaseCalls);
    }

    [Fact]
    public void ToggleSlotCommand_BookedSlot_CallsReleaseSlotAsync()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (specialistId, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee")));
        var commandService = new StubCalendarCommandService();
        var sut = new CalendarPageViewModel(queryService, commandService);
        var slot = MakeSlot("specialist-1", AvailabilityStatus.Booked);

        sut.ToggleSlotCommand.Execute(slot);

        var call = Assert.Single(commandService.ReleaseCalls);
        Assert.Equal("specialist-1", call.SpecialistId);
        Assert.Empty(commandService.ReserveCalls);
    }

    [Fact]
    public void LoadCommand_ExecutedAfterFailure_RecoversToLoadedState()
    {
        var specialists = new List<ScheduledSpecialistDto> { MakeSpecialist("specialist-1", "Jordan Lee") };
        var shouldFail = true;
        var slot = MakeSlot("specialist-1", AvailabilityStatus.Available);
        var queryService = new StubCalendarQueryService(
            _ => Task.FromResult<IReadOnlyList<ScheduledSpecialistDto>>(specialists),
            (specialistId, _, _) => shouldFail
                ? Task.FromException<DailyAvailabilityDto>(new InvalidOperationException("boom"))
                : Task.FromResult(MakeAvailability(specialistId, "Jordan Lee", [slot])));
        var sut = new CalendarPageViewModel(queryService, new StubCalendarCommandService());
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
            (specialistId, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee")));

        var sut = new CalendarPageViewModel(queryService, new StubCalendarCommandService());

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
            (specialistId, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee", [slot])));
        var sut = new CalendarPageViewModel(queryService, new StubCalendarCommandService());

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
            (specialistId, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee", [slot])));
        var sut = new CalendarPageViewModel(queryService, new StubCalendarCommandService());
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
            (specialistId, _, _) => Task.FromResult(MakeAvailability(specialistId, "Jordan Lee")));
        var sut = new CalendarPageViewModel(queryService, new StubCalendarCommandService());

        sut.SetViewModeCommand.Execute(CalendarViewMode.Week);

        Assert.Equal(DashboardState.Empty, sut.State);
    }
}
