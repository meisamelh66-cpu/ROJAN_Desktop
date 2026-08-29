using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Specialists.Schedule;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Specialists;

/// <summary>
/// Phase 7.2.6 Shift Engine UI Activation - the Manager schedule UI: full
/// read/write access to one specialist's weekly availability, overrides,
/// leave, and blocks. Owned by <see cref="SpecialistProfileViewModel"/>,
/// constructed fresh whenever the selected specialist changes - same
/// per-selection child-ViewModel pattern that class's own doc comment
/// establishes.
///
/// No domain/repository/permission change - this is Presentation only,
/// consuming <see cref="ISpecialistScheduleQueryService"/>/
/// <see cref="ISpecialistScheduleCommandService"/> exactly as Phase 7.2.4
/// left them. All input validation here (date/time text parsing) is
/// presentation-layer input handling, not a business rule - a malformed
/// value is rejected locally with <see cref="InputErrorMessage"/> before
/// ever reaching the command service; no conflict/availability rule is
/// evaluated here, matching <see cref="Domain.Specialists.Schedule.ISpecialistScheduleRepository"/>'s
/// own "must not validate conflicts" boundary.
///
/// <see cref="IsPermissionDenied"/> is a distinct state from
/// <see cref="ErrorMessage"/>/<see cref="State"/> - a
/// <see cref="UnauthorizedOperationException"/> from
/// <see cref="ISpecialistScheduleCommandService"/> (thrown by
/// <c>SpecialistScheduleCommandServicePermissionGate</c>, per Phase 7.2.4)
/// means "you are not allowed to do this," not "something went wrong,"
/// and is shown as its own message rather than the generic error state -
/// same reasoning the Implementation Readiness report's own §E called for.
///
/// Phase 7.4.1 Production Hardening: every caught failure is now also
/// logged (<see cref="LogPermissionDenied"/>/<see cref="LogOperationFailed"/>,
/// same allocation-free <c>[LoggerMessage]</c> pattern already established
/// by <c>Infrastructure.Api.HttpApiClient</c>) - before this, a failure
/// handled here (shown via <see cref="ErrorMessage"/>/<see cref="IsPermissionDenied"/>)
/// left no diagnostic trail at all; only a genuinely *unhandled* exception
/// ever reached the app's global logger. <see cref="ILogger{T}"/> is
/// optional, defaulting to <see cref="NullLogger{T}.Instance"/> when not
/// supplied - deliberately, so this addition doesn't force every existing
/// test constructing this class to change; tests that want to assert on
/// logging can still inject a real one.
/// </summary>
public sealed partial class SpecialistScheduleViewModel : ViewModelBase
{
    private readonly string _specialistId;
    private readonly ISpecialistScheduleQueryService _queryService;
    private readonly ISpecialistScheduleCommandService _commandService;
    private readonly ILogger<SpecialistScheduleViewModel> _logger;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private bool _isPermissionDenied;
    private string? _inputErrorMessage;

    private DayOfWeek _selectedDayOfWeek = DayOfWeek.Monday;
    private string _newAvailabilityStartText = string.Empty;
    private string _newAvailabilityEndText = string.Empty;

    private string _newOverrideDateText = string.Empty;
    private string _newOverrideStartText = string.Empty;
    private string _newOverrideEndText = string.Empty;
    private string _newOverrideReason = string.Empty;

    private string _newLeaveStartDateText = string.Empty;
    private string _newLeaveEndDateText = string.Empty;
    private string _newLeaveReason = string.Empty;

    private string _newBlockDateText = string.Empty;
    private string _newBlockStartText = string.Empty;
    private string _newBlockEndText = string.Empty;
    private string _newBlockReason = string.Empty;

    public SpecialistScheduleViewModel(string specialistId, ISpecialistScheduleQueryService queryService, ISpecialistScheduleCommandService commandService, ILogger<SpecialistScheduleViewModel>? logger = null)
    {
        _specialistId = specialistId;
        _queryService = queryService;
        _commandService = commandService;
        _logger = logger ?? NullLogger<SpecialistScheduleViewModel>.Instance;

        WeeklyAvailability = new ObservableCollection<WeeklyAvailabilityDto>();
        Overrides = new ObservableCollection<ScheduleOverrideDto>();
        Leave = new ObservableCollection<SpecialistLeaveDto>();
        Blocks = new ObservableCollection<SpecialistBlockDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        SetWeeklyAvailabilityCommand = new AsyncRelayCommand(_ => SetWeeklyAvailabilityAsync());
        RemoveWeeklyAvailabilityCommand = new AsyncRelayCommand(parameter => RemoveWeeklyAvailabilityAsync(parameter as WeeklyAvailabilityDto));
        SetOverrideCommand = new AsyncRelayCommand(_ => SetOverrideAsync());
        RemoveOverrideCommand = new AsyncRelayCommand(parameter => RemoveOverrideAsync(parameter as ScheduleOverrideDto));
        CreateLeaveCommand = new AsyncRelayCommand(_ => CreateLeaveAsync());
        RemoveLeaveCommand = new AsyncRelayCommand(parameter => RemoveLeaveAsync(parameter as SpecialistLeaveDto));
        CreateBlockCommand = new AsyncRelayCommand(_ => CreateBlockAsync());
        RemoveBlockCommand = new AsyncRelayCommand(parameter => RemoveBlockAsync(parameter as SpecialistBlockDto));

        // Safe fire-and-forget: LoadAsync catches every failure internally and represents it via
        // State/ErrorMessage/IsPermissionDenied, same pattern as SpecialistProfileViewModel.
        _ = LoadAsync();
    }

    public ObservableCollection<WeeklyAvailabilityDto> WeeklyAvailability { get; }

    public ObservableCollection<ScheduleOverrideDto> Overrides { get; }

    public ObservableCollection<SpecialistLeaveDto> Leave { get; }

    public ObservableCollection<SpecialistBlockDto> Blocks { get; }

    public IReadOnlyList<DayOfWeek> AvailableDaysOfWeek { get; } = Enum.GetValues<DayOfWeek>();

    public ICommand LoadCommand { get; }

    public ICommand SetWeeklyAvailabilityCommand { get; }

    public ICommand RemoveWeeklyAvailabilityCommand { get; }

    public ICommand SetOverrideCommand { get; }

    public ICommand RemoveOverrideCommand { get; }

    public ICommand CreateLeaveCommand { get; }

    public ICommand RemoveLeaveCommand { get; }

    public ICommand CreateBlockCommand { get; }

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

    /// <summary>True when the last load/mutation failed with <see cref="UnauthorizedOperationException"/> - see this class's own doc comment for why this is kept separate from <see cref="ErrorMessage"/>.</summary>
    public bool IsPermissionDenied
    {
        get => _isPermissionDenied;
        private set => SetProperty(ref _isPermissionDenied, value);
    }

    /// <summary>Local input-parsing failure (malformed time/date text) - never sent to the command service, never a business rule.</summary>
    public string? InputErrorMessage
    {
        get => _inputErrorMessage;
        private set => SetProperty(ref _inputErrorMessage, value);
    }

    public DayOfWeek SelectedDayOfWeek
    {
        get => _selectedDayOfWeek;
        set => SetProperty(ref _selectedDayOfWeek, value);
    }

    public string NewAvailabilityStartText
    {
        get => _newAvailabilityStartText;
        set => SetProperty(ref _newAvailabilityStartText, value);
    }

    public string NewAvailabilityEndText
    {
        get => _newAvailabilityEndText;
        set => SetProperty(ref _newAvailabilityEndText, value);
    }

    public string NewOverrideDateText
    {
        get => _newOverrideDateText;
        set => SetProperty(ref _newOverrideDateText, value);
    }

    /// <summary>Empty is valid and deliberate here - an override with no interval means "unavailable all day," the same real state <see cref="Domain.Specialists.Schedule.ScheduleOverride"/>'s own doc comment describes.</summary>
    public string NewOverrideStartText
    {
        get => _newOverrideStartText;
        set => SetProperty(ref _newOverrideStartText, value);
    }

    public string NewOverrideEndText
    {
        get => _newOverrideEndText;
        set => SetProperty(ref _newOverrideEndText, value);
    }

    public string NewOverrideReason
    {
        get => _newOverrideReason;
        set => SetProperty(ref _newOverrideReason, value);
    }

    public string NewLeaveStartDateText
    {
        get => _newLeaveStartDateText;
        set => SetProperty(ref _newLeaveStartDateText, value);
    }

    public string NewLeaveEndDateText
    {
        get => _newLeaveEndDateText;
        set => SetProperty(ref _newLeaveEndDateText, value);
    }

    public string NewLeaveReason
    {
        get => _newLeaveReason;
        set => SetProperty(ref _newLeaveReason, value);
    }

    public string NewBlockDateText
    {
        get => _newBlockDateText;
        set => SetProperty(ref _newBlockDateText, value);
    }

    public string NewBlockStartText
    {
        get => _newBlockStartText;
        set => SetProperty(ref _newBlockStartText, value);
    }

    public string NewBlockEndText
    {
        get => _newBlockEndText;
        set => SetProperty(ref _newBlockEndText, value);
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
        IsPermissionDenied = false;

        try
        {
            var availability = await _queryService.GetWeeklyAvailabilityAsync(_specialistId).ConfigureAwait(true);
            var overrides = await _queryService.GetOverridesAsync(_specialistId).ConfigureAwait(true);
            var leave = await _queryService.GetLeaveAsync(_specialistId).ConfigureAwait(true);
            var blocks = await _queryService.GetBlocksAsync(_specialistId).ConfigureAwait(true);

            Replace(WeeklyAvailability, availability);
            Replace(Overrides, overrides);
            Replace(Leave, leave);
            Replace(Blocks, blocks);

            // Empty is a real, distinct state (nothing configured yet), not an error - see the
            // Domain/Application layers' own doc comments for this same "empty is valid" rule.
            State = availability.Count == 0 && overrides.Count == 0 && leave.Count == 0 && blocks.Count == 0
                ? DashboardState.Empty
                : DashboardState.Loaded;
        }
        catch (UnauthorizedOperationException)
        {
            IsPermissionDenied = true;
            State = DashboardState.Error;
            LogPermissionDenied(nameof(LoadAsync));
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page/profile ViewModel in this app (see SpecialistProfileViewModel.LoadAsync).
        catch (Exception)
#pragma warning restore CA1031
        {
            ErrorMessage = Strings.Common_ActionFailedMessage;
            State = DashboardState.Error;
            LogOperationFailed(nameof(LoadAsync));
        }
    }

    private async Task SetWeeklyAvailabilityAsync()
    {
        if (!TryParseTime(NewAvailabilityStartText, out var start) || !TryParseTime(NewAvailabilityEndText, out var end))
        {
            InputErrorMessage = Strings.Specialists_Schedule_InvalidInput;
            return;
        }

        InputErrorMessage = null;

        if (!await TryMutateAsync(() => _commandService.SetWeeklyAvailabilityAsync(_specialistId, SelectedDayOfWeek, [new TimeIntervalDto(start, end)])).ConfigureAwait(true))
        {
            return;
        }

        NewAvailabilityStartText = string.Empty;
        NewAvailabilityEndText = string.Empty;
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task RemoveWeeklyAvailabilityAsync(WeeklyAvailabilityDto? availability)
    {
        if (availability is null)
        {
            return;
        }

        if (await TryMutateAsync(() => _commandService.RemoveWeeklyAvailabilityAsync(_specialistId, availability.DayOfWeek)).ConfigureAwait(true))
        {
            await LoadAsync().ConfigureAwait(true);
        }
    }

    private async Task SetOverrideAsync()
    {
        if (!TryParseDate(NewOverrideDateText, out var date))
        {
            InputErrorMessage = Strings.Specialists_Schedule_InvalidInput;
            return;
        }

        // Both start/end blank is the deliberate "unavailable all day" case - not a parse failure.
        var bothBlank = string.IsNullOrWhiteSpace(NewOverrideStartText) && string.IsNullOrWhiteSpace(NewOverrideEndText);
        IReadOnlyList<TimeIntervalDto> intervals = [];

        if (!bothBlank)
        {
            if (!TryParseTime(NewOverrideStartText, out var start) || !TryParseTime(NewOverrideEndText, out var end))
            {
                InputErrorMessage = Strings.Specialists_Schedule_InvalidInput;
                return;
            }

            intervals = [new TimeIntervalDto(start, end)];
        }

        InputErrorMessage = null;
        var reason = string.IsNullOrWhiteSpace(NewOverrideReason) ? null : NewOverrideReason;

        if (!await TryMutateAsync(() => _commandService.SetOverrideAsync(_specialistId, date, intervals, reason)).ConfigureAwait(true))
        {
            return;
        }

        NewOverrideDateText = string.Empty;
        NewOverrideStartText = string.Empty;
        NewOverrideEndText = string.Empty;
        NewOverrideReason = string.Empty;
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task RemoveOverrideAsync(ScheduleOverrideDto? @override)
    {
        if (@override is null)
        {
            return;
        }

        if (await TryMutateAsync(() => _commandService.RemoveOverrideAsync(_specialistId, @override.Id)).ConfigureAwait(true))
        {
            await LoadAsync().ConfigureAwait(true);
        }
    }

    private async Task CreateLeaveAsync()
    {
        if (!TryParseDate(NewLeaveStartDateText, out var start) || !TryParseDate(NewLeaveEndDateText, out var end))
        {
            InputErrorMessage = Strings.Specialists_Schedule_InvalidInput;
            return;
        }

        InputErrorMessage = null;
        var reason = string.IsNullOrWhiteSpace(NewLeaveReason) ? null : NewLeaveReason;

        if (!await TryMutateAsync(() => _commandService.CreateLeaveAsync(_specialistId, start, end, reason)).ConfigureAwait(true))
        {
            return;
        }

        NewLeaveStartDateText = string.Empty;
        NewLeaveEndDateText = string.Empty;
        NewLeaveReason = string.Empty;
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task RemoveLeaveAsync(SpecialistLeaveDto? leave)
    {
        if (leave is null)
        {
            return;
        }

        if (await TryMutateAsync(() => _commandService.RemoveLeaveAsync(_specialistId, leave.Id)).ConfigureAwait(true))
        {
            await LoadAsync().ConfigureAwait(true);
        }
    }

    private async Task CreateBlockAsync()
    {
        if (!TryParseDate(NewBlockDateText, out var date) || !TryParseTime(NewBlockStartText, out var start) || !TryParseTime(NewBlockEndText, out var end))
        {
            InputErrorMessage = Strings.Specialists_Schedule_InvalidInput;
            return;
        }

        InputErrorMessage = null;
        var reason = string.IsNullOrWhiteSpace(NewBlockReason) ? null : NewBlockReason;

        if (!await TryMutateAsync(() => _commandService.CreateBlockAsync(_specialistId, date, new TimeIntervalDto(start, end), reason)).ConfigureAwait(true))
        {
            return;
        }

        NewBlockDateText = string.Empty;
        NewBlockStartText = string.Empty;
        NewBlockEndText = string.Empty;
        NewBlockReason = string.Empty;
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task RemoveBlockAsync(SpecialistBlockDto? block)
    {
        if (block is null)
        {
            return;
        }

        if (await TryMutateAsync(() => _commandService.RemoveBlockAsync(_specialistId, block.Id)).ConfigureAwait(true))
        {
            await LoadAsync().ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Shared mutation error boundary - <see cref="UnauthorizedOperationException"/>
    /// sets <see cref="IsPermissionDenied"/> (never crashes, never a generic
    /// error), any other failure sets <see cref="ErrorMessage"/>. Returns
    /// whether the mutation actually succeeded, so callers know whether to
    /// clear their input buffers and reload. <paramref name="operationName"/>
    /// is deliberately <see cref="CallerMemberNameAttribute"/>-supplied, not
    /// passed explicitly by any of the eight callers - keeps this Phase
    /// 7.4.1 logging addition from touching any of those existing call sites.
    /// </summary>
    private async Task<bool> TryMutateAsync(Func<Task> mutate, [CallerMemberName] string operationName = "")
    {
        try
        {
            await mutate().ConfigureAwait(true);
            IsPermissionDenied = false;
            ErrorMessage = null;
            return true;
        }
        catch (UnauthorizedOperationException)
        {
            IsPermissionDenied = true;
            LogPermissionDenied(operationName);
            return false;
        }
#pragma warning disable CA1031 // Mutation boundary: any failure must surface as ErrorMessage, never crash - same justified broad catch as SpecialistProfileViewModel's own save/assignment boundaries.
        catch (Exception)
#pragma warning restore CA1031
        {
            ErrorMessage = Strings.Common_ActionFailedMessage;
            LogOperationFailed(operationName);
            return false;
        }
    }

    // Operation name only: neither the caught exception nor the specialist id is
    // passed to the logger (Phase 8.15+ security rule - no backend bodies, no identifiers).
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Specialist schedule permission denied. Operation={Operation}")]
    private partial void LogPermissionDenied(string operation);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Specialist schedule operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);

    private static bool TryParseTime(string text, out TimeSpan value) =>
        TimeSpan.TryParseExact(text.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out value);

    private static bool TryParseDate(string text, out DateOnly value) =>
        DateOnly.TryParseExact(text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value);

    private static void Replace<T>(ObservableCollection<T> collection, IReadOnlyList<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
