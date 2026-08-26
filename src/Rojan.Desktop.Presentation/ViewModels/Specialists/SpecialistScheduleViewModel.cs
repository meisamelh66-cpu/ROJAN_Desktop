using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Rojan.Desktop.Application.Schedule;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Specialists;

/// <summary>
/// Phase 5 Shift Engine: drives the Schedule section of a specialist's
/// profile - weekly availability, one-off overrides, leave, and ad-hoc
/// blocks. Every value comes from <see cref="IScheduleQueryService"/>/
/// <see cref="IScheduleCommandService"/>, real and Backend-authoritative
/// (<c>SpecialistScheduleController</c>) - this class computes nothing,
/// validates nothing, and decides no conflict; it only requests, displays,
/// and submits. Same "constructed fresh per selected specialist, load-then-
/// render, broad top-level catch surfaced as State/ErrorMessage" shape as
/// <see cref="SpecialistProfileViewModel"/>, which owns and constructs this
/// class.
///
/// Weekly availability editing is scoped to exactly one interval per day
/// per save, same deliberate v1 scope limit the ROJAN Website's own Working
/// Hours feature already established for the equivalent Salon-level
/// concept - the real Backend supports multiple intervals per day (e.g. a
/// lunch-break split), and a day that already has more than one is still
/// displayed correctly (read-only list), only the edit action collapses to
/// one - a real, honest limitation, not a bug.
/// </summary>
public sealed class SpecialistScheduleViewModel : ViewModelBase
{
    private readonly string _specialistId;
    private readonly IScheduleQueryService _queryService;
    private readonly IScheduleCommandService _commandService;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;

    private DayOfWeek? _editingDay;
    private string _editIntervalStart = string.Empty;
    private string _editIntervalEnd = string.Empty;

    private string _newOverrideDate = string.Empty;
    private string _newOverrideStart = string.Empty;
    private string _newOverrideEnd = string.Empty;
    private string _newOverrideReason = string.Empty;

    private string _newLeaveStartDate = string.Empty;
    private string _newLeaveEndDate = string.Empty;
    private string _newLeaveReason = string.Empty;

    private string _newBlockDate = string.Empty;
    private string _newBlockStart = string.Empty;
    private string _newBlockEnd = string.Empty;
    private string _newBlockReason = string.Empty;

    public SpecialistScheduleViewModel(string specialistId, IScheduleQueryService queryService, IScheduleCommandService commandService)
    {
        _specialistId = specialistId;
        _queryService = queryService;
        _commandService = commandService;

        WeeklyAvailability = new ObservableCollection<ScheduleDayRow>(
            Enum.GetValues<DayOfWeek>().Select(day => new ScheduleDayRow(day)));
        Overrides = new ObservableCollection<ScheduleOverrideDto>();
        Leaves = new ObservableCollection<SpecialistLeaveDto>();
        Blocks = new ObservableCollection<SpecialistBlockDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        BeginEditDayCommand = new AsyncRelayCommand(parameter => BeginEditDayAsync(parameter as ScheduleDayRow));
        SaveDayAvailabilityCommand = new AsyncRelayCommand(_ => SaveDayAvailabilityAsync());
        ClearDayAvailabilityCommand = new AsyncRelayCommand(parameter => ClearDayAvailabilityAsync(parameter as ScheduleDayRow));

        AddOverrideCommand = new AsyncRelayCommand(_ => AddOverrideAsync());
        RemoveOverrideCommand = new AsyncRelayCommand(parameter => RemoveOverrideAsync(parameter as ScheduleOverrideDto));

        AddLeaveCommand = new AsyncRelayCommand(_ => AddLeaveAsync());
        RemoveLeaveCommand = new AsyncRelayCommand(parameter => RemoveLeaveAsync(parameter as SpecialistLeaveDto));

        AddBlockCommand = new AsyncRelayCommand(_ => AddBlockAsync());
        RemoveBlockCommand = new AsyncRelayCommand(parameter => RemoveBlockAsync(parameter as SpecialistBlockDto));

        // Safe fire-and-forget: LoadAsync catches every failure internally and represents it via
        // State/ErrorMessage, same pattern as every other page/profile ViewModel in this app.
        _ = LoadAsync();
    }

    public ObservableCollection<ScheduleDayRow> WeeklyAvailability { get; }

    public ObservableCollection<ScheduleOverrideDto> Overrides { get; }

    public ObservableCollection<SpecialistLeaveDto> Leaves { get; }

    public ObservableCollection<SpecialistBlockDto> Blocks { get; }

    public ICommand LoadCommand { get; }

    public ICommand BeginEditDayCommand { get; }

    public ICommand SaveDayAvailabilityCommand { get; }

    public ICommand ClearDayAvailabilityCommand { get; }

    public ICommand AddOverrideCommand { get; }

    public ICommand RemoveOverrideCommand { get; }

    public ICommand AddLeaveCommand { get; }

    public ICommand RemoveLeaveCommand { get; }

    public ICommand AddBlockCommand { get; }

    public ICommand RemoveBlockCommand { get; }

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

    /// <summary>Which day's row currently has its edit form open - null when none does. Bound by each row's own visibility trigger.</summary>
    public DayOfWeek? EditingDay
    {
        get => _editingDay;
        private set => SetProperty(ref _editingDay, value);
    }

    public string EditIntervalStart
    {
        get => _editIntervalStart;
        set => SetProperty(ref _editIntervalStart, value);
    }

    public string EditIntervalEnd
    {
        get => _editIntervalEnd;
        set => SetProperty(ref _editIntervalEnd, value);
    }

    public string NewOverrideDate
    {
        get => _newOverrideDate;
        set => SetProperty(ref _newOverrideDate, value);
    }

    public string NewOverrideStart
    {
        get => _newOverrideStart;
        set => SetProperty(ref _newOverrideStart, value);
    }

    public string NewOverrideEnd
    {
        get => _newOverrideEnd;
        set => SetProperty(ref _newOverrideEnd, value);
    }

    public string NewOverrideReason
    {
        get => _newOverrideReason;
        set => SetProperty(ref _newOverrideReason, value);
    }

    public string NewLeaveStartDate
    {
        get => _newLeaveStartDate;
        set => SetProperty(ref _newLeaveStartDate, value);
    }

    public string NewLeaveEndDate
    {
        get => _newLeaveEndDate;
        set => SetProperty(ref _newLeaveEndDate, value);
    }

    public string NewLeaveReason
    {
        get => _newLeaveReason;
        set => SetProperty(ref _newLeaveReason, value);
    }

    public string NewBlockDate
    {
        get => _newBlockDate;
        set => SetProperty(ref _newBlockDate, value);
    }

    public string NewBlockStart
    {
        get => _newBlockStart;
        set => SetProperty(ref _newBlockStart, value);
    }

    public string NewBlockEnd
    {
        get => _newBlockEnd;
        set => SetProperty(ref _newBlockEnd, value);
    }

    public string NewBlockReason
    {
        get => _newBlockReason;
        set => SetProperty(ref _newBlockReason, value);
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var availability = await _queryService.GetWeeklyAvailabilityAsync(_specialistId).ConfigureAwait(true);
            foreach (var row in WeeklyAvailability)
            {
                row.Availability = availability.FirstOrDefault(entry => entry.DayOfWeek == row.DayOfWeek);
            }

            var overrides = await _queryService.GetOverridesAsync(_specialistId).ConfigureAwait(true);
            Overrides.Clear();
            foreach (var entry in overrides.OrderBy(entry => entry.Date))
            {
                Overrides.Add(entry);
            }

            var leaves = await _queryService.GetLeavesAsync(_specialistId).ConfigureAwait(true);
            Leaves.Clear();
            foreach (var entry in leaves.OrderBy(entry => entry.StartDate))
            {
                Leaves.Add(entry);
            }

            var blocks = await _queryService.GetBlocksAsync(_specialistId).ConfigureAwait(true);
            Blocks.Clear();
            foreach (var entry in blocks.OrderBy(entry => entry.Date))
            {
                Blocks.Add(entry);
            }

            State = DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page/profile ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
        }
    }

    private Task BeginEditDayAsync(ScheduleDayRow? row)
    {
        if (row is null)
        {
            return Task.CompletedTask;
        }

        EditingDay = row.DayOfWeek;
        foreach (var candidate in WeeklyAvailability)
        {
            candidate.IsEditing = candidate.DayOfWeek == row.DayOfWeek;
        }

        var intervals = row.Availability?.Intervals;
        var firstInterval = intervals is { Count: > 0 } ? intervals[0] : null;
        EditIntervalStart = firstInterval is null ? string.Empty : firstInterval.Start.ToString("HH:mm", CultureInfo.InvariantCulture);
        EditIntervalEnd = firstInterval is null ? string.Empty : firstInterval.End.ToString("HH:mm", CultureInfo.InvariantCulture);
        return Task.CompletedTask;
    }

    private async Task SaveDayAvailabilityAsync()
    {
        if (EditingDay is not { } day)
        {
            return;
        }

        if (!TimeOnly.TryParse(EditIntervalStart, CultureInfo.InvariantCulture, out var start) ||
            !TimeOnly.TryParse(EditIntervalEnd, CultureInfo.InvariantCulture, out var end))
        {
            ErrorMessage = Localization.Strings.SpecialistSchedule_InvalidTimeRange;
            State = DashboardState.Error;
            return;
        }

        await _commandService.SetWeeklyAvailabilityAsync(_specialistId, day, [new TimeIntervalDto(start, end)]).ConfigureAwait(true);
        EditingDay = null;
        foreach (var candidate in WeeklyAvailability)
        {
            candidate.IsEditing = false;
        }
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task ClearDayAvailabilityAsync(ScheduleDayRow? row)
    {
        if (row is null)
        {
            return;
        }

        await _commandService.RemoveWeeklyAvailabilityAsync(_specialistId, row.DayOfWeek).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task AddOverrideAsync()
    {
        if (!DateOnly.TryParse(NewOverrideDate, CultureInfo.InvariantCulture, out var date))
        {
            return;
        }

        IReadOnlyList<TimeIntervalDto> intervals = [];
        if (TimeOnly.TryParse(NewOverrideStart, CultureInfo.InvariantCulture, out var start) &&
            TimeOnly.TryParse(NewOverrideEnd, CultureInfo.InvariantCulture, out var end))
        {
            intervals = [new TimeIntervalDto(start, end)];
        }

        await _commandService.SetOverrideAsync(_specialistId, date, intervals, NullIfEmpty(NewOverrideReason)).ConfigureAwait(true);
        NewOverrideDate = string.Empty;
        NewOverrideStart = string.Empty;
        NewOverrideEnd = string.Empty;
        NewOverrideReason = string.Empty;
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task RemoveOverrideAsync(ScheduleOverrideDto? entry)
    {
        if (entry is null)
        {
            return;
        }

        await _commandService.RemoveOverrideAsync(_specialistId, entry.Id).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task AddLeaveAsync()
    {
        if (!DateOnly.TryParse(NewLeaveStartDate, CultureInfo.InvariantCulture, out var startDate) ||
            !DateOnly.TryParse(NewLeaveEndDate, CultureInfo.InvariantCulture, out var endDate))
        {
            return;
        }

        await _commandService.CreateLeaveAsync(_specialistId, startDate, endDate, NullIfEmpty(NewLeaveReason)).ConfigureAwait(true);
        NewLeaveStartDate = string.Empty;
        NewLeaveEndDate = string.Empty;
        NewLeaveReason = string.Empty;
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task RemoveLeaveAsync(SpecialistLeaveDto? entry)
    {
        if (entry is null)
        {
            return;
        }

        await _commandService.RemoveLeaveAsync(_specialistId, entry.Id).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task AddBlockAsync()
    {
        if (!DateOnly.TryParse(NewBlockDate, CultureInfo.InvariantCulture, out var date) ||
            !TimeOnly.TryParse(NewBlockStart, CultureInfo.InvariantCulture, out var start) ||
            !TimeOnly.TryParse(NewBlockEnd, CultureInfo.InvariantCulture, out var end))
        {
            return;
        }

        await _commandService.CreateBlockAsync(_specialistId, date, start, end, NullIfEmpty(NewBlockReason)).ConfigureAwait(true);
        NewBlockDate = string.Empty;
        NewBlockStart = string.Empty;
        NewBlockEnd = string.Empty;
        NewBlockReason = string.Empty;
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task RemoveBlockAsync(SpecialistBlockDto? entry)
    {
        if (entry is null)
        {
            return;
        }

        await _commandService.RemoveBlockAsync(_specialistId, entry.Id).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

/// <summary>One real .NET <see cref="DayOfWeek"/> row for the weekly-availability list - <see cref="Availability"/> is null when the specialist has no real Backend entry for this day yet ("closed"/not configured), never a fabricated default.</summary>
public sealed class ScheduleDayRow(DayOfWeek dayOfWeek) : ViewModelBase
{
    private WeeklyAvailabilityDto? _availability;
    private bool _isEditing;

    public DayOfWeek DayOfWeek { get; } = dayOfWeek;

    public WeeklyAvailabilityDto? Availability
    {
        get => _availability;
        set => SetProperty(ref _availability, value);
    }

    /// <summary>True only for the one row currently showing its inline edit form - set by <see cref="SpecialistScheduleViewModel"/>, never more than one row at a time.</summary>
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }
}
