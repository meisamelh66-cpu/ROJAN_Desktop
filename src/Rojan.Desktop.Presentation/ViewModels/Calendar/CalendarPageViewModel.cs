using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Rojan.Desktop.Application.Calendar;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Calendar;

/// <summary>
/// Drives CalendarPage - a specialist/date picker plus the generated
/// daily availability grid, with a single toggle command that reserves an
/// Available slot or releases a Booked one. Depends only on Application
/// services (<see cref="ICalendarQueryService"/>, <see cref="ICalendarCommandService"/>),
/// consistent with Presentation never reaching past Application into
/// Domain/Infrastructure. Reuses <see cref="DashboardState"/> rather than
/// a duplicate enum, same reasoning as every other page ViewModel in this
/// app. Two-stage load: first the scheduled-specialist pick-list, then
/// (once a specialist is selected) that specialist's daily availability -
/// unlike every other module, there is no list-plus-per-selection-profile
/// split here, since a day's availability grid *is* the page, not a detail
/// panel for a selected row.
/// </summary>
public sealed class CalendarPageViewModel : ViewModelBase
{
    private readonly ICalendarQueryService _queryService;
    private readonly ICalendarCommandService _commandService;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private ScheduledSpecialistDto? _selectedSpecialist;
    private DateTime _selectedDate = DateTime.Today.AddDays(1);
    private string _workingHoursText = string.Empty;

    public CalendarPageViewModel(ICalendarQueryService queryService, ICalendarCommandService commandService)
    {
        _queryService = queryService;
        _commandService = commandService;

        Specialists = new ObservableCollection<ScheduledSpecialistDto>();
        Slots = new ObservableCollection<AvailabilitySlotDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAvailabilityAsync());
        ToggleSlotCommand = new AsyncRelayCommand(parameter => ToggleSlotAsync(parameter as AvailabilitySlotDto));

        // Safe fire-and-forget: InitializeAsync catches every failure
        // internally and represents it via State/ErrorMessage, so there is
        // nothing left that could become an unobserved task exception.
        _ = InitializeAsync();
    }

    public ObservableCollection<ScheduledSpecialistDto> Specialists { get; }

    public ObservableCollection<AvailabilitySlotDto> Slots { get; }

    /// <summary>Re-runs the availability load - bound as the Retry action on DashboardWidget's Error state.</summary>
    public ICommand LoadCommand { get; }

    public ICommand ToggleSlotCommand { get; }

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

            // Setting SelectedSpecialist triggers LoadAvailabilityAsync via its own setter.
            SelectedSpecialist = Specialists[0];
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
        }
    }

    private async Task LoadAvailabilityAsync()
    {
        if (SelectedSpecialist is null)
        {
            State = DashboardState.Empty;
            return;
        }

        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var scheduleDate = DateOnly.FromDateTime(SelectedDate);
            var availability = await _queryService.GetDailyAvailabilityAsync(SelectedSpecialist.Id, scheduleDate).ConfigureAwait(true);

            WorkingHoursText = availability.WorkingStart.HasValue && availability.WorkingEnd.HasValue
                ? $"Working {FormatTime(availability.WorkingStart.Value)} - {FormatTime(availability.WorkingEnd.Value)}"
                : "Not scheduled to work this day.";

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
        }
    }

    private async Task ToggleSlotAsync(AvailabilitySlotDto? slot)
    {
        if (slot is null || SelectedSpecialist is null)
        {
            return;
        }

        switch (slot.Status)
        {
            case AvailabilityStatus.Available:
                await _commandService.ReserveSlotAsync(SelectedSpecialist.Id, slot.Start, slot.End).ConfigureAwait(true);
                break;
            case AvailabilityStatus.Booked:
                await _commandService.ReleaseSlotAsync(SelectedSpecialist.Id, slot.Start, slot.End).ConfigureAwait(true);
                break;
            case AvailabilityStatus.Unavailable:
            default:
                return;
        }

        await LoadAvailabilityAsync().ConfigureAwait(true);
    }

    private static string FormatTime(TimeSpan time) =>
        DateTime.Today.Add(time).ToString("h:mm tt", CultureInfo.InvariantCulture);
}
