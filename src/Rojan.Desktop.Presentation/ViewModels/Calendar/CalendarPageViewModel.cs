using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Calendar;
using Rojan.Desktop.Application.Services;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Calendar;

/// <summary>
/// Drives CalendarPage - a specialist/service/date picker plus the loaded
/// availability grid (Day or Week, via <see cref="ViewMode"/> - Sprint 2
/// Commit 4). Depends only on Application services
/// (<see cref="ICalendarQueryService"/>, <see cref="IServiceQueryService"/>),
/// consistent with Presentation never reaching past Application into
/// Domain/Infrastructure. Reuses <see cref="DashboardState"/> rather than a
/// duplicate enum, same reasoning as every other page ViewModel in this
/// app. Three-stage load: the scheduled-specialist pick-list, the active
/// service catalog, then (once both are selected) their availability -
/// unlike every other module, there is no list-plus-detail split here,
/// since the availability grid *is* the page, not a detail panel for a
/// selected row.
///
/// Calendar/Availability Integration Phase 3 (Service-driven Calendar
/// flow, product decision): this page is now read-only. The manual
/// "toggle a slot to Booked with no customer/service attached" feature -
/// and this ViewModel's <c>ICalendarCommandService</c> dependency along
/// with it - is gone; a slot's Booked state now comes from real
/// <c>Booking</c> data only (via the Booking Wizard), never a local
/// reservation this page wrote itself. Remediation Phase 3A (Calendar
/// Dead Code Cleanup) later removed <c>ICalendarCommandService</c> itself
/// (and its entire local-storage-backed implementation) from the codebase
/// entirely, confirming it had zero remaining callers anywhere - see
/// ROJAN_DESKTOP_CALENDAR_CLEANUP_PHASE3A_REPORT_v1.md.
/// <see cref="ICalendarQueryService"/>'s backend-connected implementation
/// requires a service to compute slot length (see that interface's own
/// doc comment), hence the new <see cref="Services"/>/<see cref="SelectedService"/>
/// - a real, deliberate scope addition to this page, not incidental
/// plumbing.
/// </summary>
public sealed partial class CalendarPageViewModel : ViewModelBase
{
    private readonly ICalendarQueryService _queryService;
    private readonly IServiceQueryService _serviceQueryService;
    private readonly ILogger<CalendarPageViewModel> _logger;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private ScheduledSpecialistDto? _selectedSpecialist;
    private ServiceDto? _selectedService;
    private DateTime _selectedDate = DateTime.Today.AddDays(1);
    private string _workingHoursText = string.Empty;
    private CalendarViewMode _viewMode = CalendarViewMode.Day;

    public CalendarPageViewModel(ICalendarQueryService queryService, IServiceQueryService serviceQueryService, ILogger<CalendarPageViewModel>? logger = null)
    {
        _queryService = queryService;
        _serviceQueryService = serviceQueryService;
        _logger = logger ?? NullLogger<CalendarPageViewModel>.Instance;

        Specialists = new ObservableCollection<ScheduledSpecialistDto>();
        Services = new ObservableCollection<ServiceDto>();
        Slots = new ObservableCollection<AvailabilitySlotDto>();
        WeekDays = new ObservableCollection<DailyAvailabilityDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAvailabilityAsync());
        SetViewModeCommand = new RelayCommand(parameter =>
        {
            if (parameter is CalendarViewMode mode)
            {
                ViewMode = mode;
            }
        });

        // Safe fire-and-forget: InitializeAsync catches every failure
        // internally and represents it via State/ErrorMessage, so there is
        // nothing left that could become an unobserved task exception.
        _ = InitializeAsync();
    }

    public ObservableCollection<ScheduledSpecialistDto> Specialists { get; }

    /// <summary>Active services only, same filter <c>BookingWorkflowService.GetBookingOptionsAsync</c> already applies - a discontinued/draft service has no meaningful slot length to request.</summary>
    public ObservableCollection<ServiceDto> Services { get; }

    /// <summary>Populated only in <see cref="CalendarViewMode.Day"/>.</summary>
    public ObservableCollection<AvailabilitySlotDto> Slots { get; }

    /// <summary>Populated only in <see cref="CalendarViewMode.Week"/> - seven <see cref="DailyAvailabilityDto"/> entries starting at <see cref="SelectedDate"/>, one per day.</summary>
    public ObservableCollection<DailyAvailabilityDto> WeekDays { get; }

    /// <summary>Re-runs the availability load - bound as the Retry action on DashboardWidget's Error state.</summary>
    public ICommand LoadCommand { get; }

    /// <summary>Switches <see cref="ViewMode"/> between Day and Week - bound as the Command on CalendarPage's two view-mode RadioButtons, parameter is a boxed <see cref="CalendarViewMode"/>.</summary>
    public ICommand SetViewModeCommand { get; }

    public DashboardState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public ScheduledSpecialistDto? SelectedSpecialist
    {
        get => _selectedSpecialist;
        set
        {
            if (SetProperty(ref _selectedSpecialist, value))
            {
                _ = LoadAvailabilityAsync();
            }
        }
    }

    public ServiceDto? SelectedService
    {
        get => _selectedService;
        set
        {
            if (SetProperty(ref _selectedService, value))
            {
                _ = LoadAvailabilityAsync();
            }
        }
    }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value))
            {
                _ = LoadAvailabilityAsync();
            }
        }
    }

    public string WorkingHoursText
    {
        get => _workingHoursText;
        private set => SetProperty(ref _workingHoursText, value);
    }

    public CalendarViewMode ViewMode
    {
        get => _viewMode;
        set
        {
            if (SetProperty(ref _viewMode, value))
            {
                OnPropertyChanged(nameof(IsDayView));
                OnPropertyChanged(nameof(IsWeekView));
                _ = LoadAvailabilityAsync();
            }
        }
    }

    public bool IsDayView => ViewMode == CalendarViewMode.Day;

    public bool IsWeekView => ViewMode == CalendarViewMode.Week;

    private async Task InitializeAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var specialists = await _queryService.GetScheduledSpecialistsAsync().ConfigureAwait(true);

            Specialists.Clear();
            foreach (var specialist in specialists)
            {
                Specialists.Add(specialist);
            }

            if (Specialists.Count == 0)
            {
                State = DashboardState.Empty;
                return;
            }

            var services = await _serviceQueryService.GetServicesAsync().ConfigureAwait(true);

            Services.Clear();
            foreach (var service in services.Where(service => service.Status == ServiceStatus.Active))
            {
                Services.Add(service);
            }

            if (Services.Count == 0)
            {
                State = DashboardState.Empty;
                return;
            }

            // Setting SelectedSpecialist triggers a load via its own setter, but with
            // SelectedService still null at that point it's a harmless early-return (see
            // LoadDailyAvailabilityAsync/LoadWeeklyAvailabilityAsync's own null guard) - the
            // real load fires once SelectedService is set next.
            SelectedSpecialist = Specialists[0];
            SelectedService = Services[0];
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
            LogLoadFailed(nameof(InitializeAsync));
        }
    }

    /// <summary>Dispatches to the load logic for the current <see cref="ViewMode"/> - the single entry point every reload trigger (specialist/service/date change, ViewMode change, LoadCommand retry) calls.</summary>
    private Task LoadAvailabilityAsync() => ViewMode == CalendarViewMode.Week
        ? LoadWeeklyAvailabilityAsync()
        : LoadDailyAvailabilityAsync();

    private async Task LoadDailyAvailabilityAsync()
    {
        if (SelectedSpecialist is null || SelectedService is null)
        {
            State = DashboardState.Empty;
            return;
        }

        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var scheduleDate = DateOnly.FromDateTime(SelectedDate);
            var availability = await _queryService.GetDailyAvailabilityAsync(SelectedSpecialist.Id, SelectedService.Id, scheduleDate).ConfigureAwait(true);

            WorkingHoursText = availability.WorkingStart.HasValue && availability.WorkingEnd.HasValue
                ? $"Working {FormatTime(availability.WorkingStart.Value)} - {FormatTime(availability.WorkingEnd.Value)}"
                : "Not scheduled to work this day.";

            WeekDays.Clear();
            Slots.Clear();
            foreach (var slot in availability.Slots)
            {
                Slots.Add(slot);
            }

            State = Slots.Count == 0 ? DashboardState.Empty : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Same top-level boundary reasoning as InitializeAsync.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
            LogLoadFailed(nameof(LoadDailyAvailabilityAsync));
        }
    }

    private async Task LoadWeeklyAvailabilityAsync()
    {
        if (SelectedSpecialist is null || SelectedService is null)
        {
            State = DashboardState.Empty;
            return;
        }

        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var weekStart = DateOnly.FromDateTime(SelectedDate);
            var availability = await _queryService.GetWeeklyAvailabilityAsync(SelectedSpecialist.Id, SelectedService.Id, weekStart).ConfigureAwait(true);

            // Week view is a per-day summary grid, not the Day view's
            // single working-hours caption - each day can have different
            // hours, so there is no one WorkingHoursText to show here.
            WorkingHoursText = string.Empty;

            Slots.Clear();
            WeekDays.Clear();
            foreach (var day in availability.Days)
            {
                WeekDays.Add(day);
            }

            State = WeekDays.All(day => day.Slots.Count == 0) ? DashboardState.Empty : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Same top-level boundary reasoning as InitializeAsync.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
            LogLoadFailed(nameof(LoadWeeklyAvailabilityAsync));
        }
    }

    // Operation name only: the caught exception is never passed to the logger
    // (Phase 8.15+ security rule - backend response bodies must not reach the log).
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Calendar availability load failed. Operation={Operation}")]
    private partial void LogLoadFailed(string operation);

    private static string FormatTime(TimeSpan time) =>
        DateTime.Today.Add(time).ToString("h:mm tt", CultureInfo.InvariantCulture);
}
