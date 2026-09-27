using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Bookings;
using Rojan.Desktop.Application.BookingWorkflow;
using Rojan.Desktop.Presentation.Dialogs;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.BookingWorkflow;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Bookings;

/// <summary>
/// Drives BookingPage - the booking list on the left, a free-text
/// new-booking quick-add form, and the selected booking's details plus
/// status-transition actions on the right. Depends only on Application
/// services (<see cref="IBookingQueryService"/>,
/// <see cref="IBookingCommandService"/>, <see cref="IBookingWorkflowService"/>)
/// plus <see cref="IDialogService"/>, consistent with Presentation never
/// reaching past Application into Domain/Infrastructure. Reuses
/// <see cref="DashboardState"/> rather than a duplicate enum, same
/// reasoning as every other page ViewModel in this app.
/// <see cref="OpenWizardCommand"/> (Phase 15) opens the guided,
/// real-cross-slice-data Booking Wizard as a dialog - the quick-add form
/// stays as-is (free text, foundation scope) alongside it, not replaced by
/// it.
/// <see cref="SearchText"/>/<see cref="CustomerNameFilter"/>/<see cref="ServiceNameFilter"/>/
/// <see cref="StatusFilter"/>/<see cref="DateFromFilter"/>/<see cref="DateToFilter"/>
/// (Sprint 3 Commit 2) are combined into one <see cref="BookingSearchFilter"/>
/// and run through <see cref="IBookingQueryService.SearchBookingsAsync"/> -
/// every load (including the initial one and every post-write reload) goes
/// through this same method now, not a separate <c>GetBookingsAsync</c>
/// path, so an active filter survives a Confirm/Complete/Cancel/Create
/// action instead of silently resetting. An all-default filter is
/// equivalent to the old unfiltered <c>GetBookingsAsync</c> call - see
/// <see cref="BookingSearchFilter"/>'s own doc comment.
///
/// Phase 7.4.4 Booking/Checkout Error Hardening: <see cref="CreateBookingAsync"/>/
/// <see cref="ChangeStatusAsync"/>/<see cref="CancelSelectedBookingAsync"/>/
/// <see cref="RescheduleSelectedBookingAsync"/> previously had no
/// try/catch at all - a real backend/network failure would propagate as
/// an unhandled exception through <c>AsyncRelayCommand.Execute</c>'s bare
/// <c>try/finally</c> and surface as the app's generic global error
/// dialog (still safely recovered by <c>Shell.App</c>'s own
/// <c>DispatcherUnhandledException</c> handler - never a crash), rather
/// than this app's own established in-page <see cref="ErrorMessage"/>/
/// <see cref="State"/> pattern every other command in this app already
/// uses. Fixed by wrapping each in the same pattern <see cref="LoadAsync"/>
/// already used, plus logging (same allocation-free <c>[LoggerMessage]</c>
/// pattern <c>Specialists.SpecialistScheduleViewModel</c> established) -
/// no change to what any of these methods call, when they are allowed to
/// run (<c>CanExecute</c> predicates, unchanged), or what
/// <c>Domain.Bookings.BookingRules</c>/the backend actually decide.
///
/// Page Stability: a failed action no longer switches the whole page to
/// <see cref="DashboardState.Error"/> - that replaced the booking list (and
/// the user's selection) with a Retry panel that only a full reload could
/// clear. A failed Create now reports via <see cref="CreateErrorMessage"/>
/// (form kept), a failed Confirm/Start/Complete/No-Show/Cancel/Reschedule via
/// <see cref="ActionErrorMessage"/> next to the details actions; the list
/// stays exactly as it was. Every post-action/filter reload of an already
/// displayed list is a refresh (<see cref="IsRefreshing"/>, see
/// <see cref="DashboardStateExtensions"/>) that keeps the list visible and
/// the selected booking selected by Id.
/// </summary>
public sealed partial class BookingPageViewModel : ViewModelBase
{
    private readonly IBookingQueryService _queryService;
    private readonly IBookingCommandService _commandService;
    private readonly IBookingWorkflowService _workflowService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<BookingPageViewModel> _logger;
    private readonly ILoggerFactory? _loggerFactory;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private bool _isRefreshing;
    private string? _actionErrorMessage;
    private string? _createErrorMessage;
    private BookingDto? _selectedBooking;
    private bool _isReplacingBookings;
    private string _newBookingCustomerName = string.Empty;
    private string _newBookingServiceName = string.Empty;
    private string _newBookingSpecialistName = string.Empty;
    private DateTime? _newBookingDate = DateTime.Today.AddDays(1);
    private int _newBookingDurationMinutes = 60;
    private DateTime? _rescheduleDate;
    private string _searchText = string.Empty;
    private string _customerNameFilter = string.Empty;
    private string _serviceNameFilter = string.Empty;
    private BookingStatus? _statusFilter;
    private DateTime? _dateFromFilter;
    private DateTime? _dateToFilter;

    /// <summary>
    /// Incremented on every filter/load-triggering change; a completed
    /// <see cref="LoadAsync"/> call discards its result if this no longer
    /// matches the version it captured when it started - generalizes
    /// <c>Customers.CustomerPageViewModel.SearchAsync</c>'s single-field
    /// stale-result guard to Booking's six independent filter fields, any
    /// of which (typed in rapid succession) could otherwise let an older,
    /// slower search response overwrite a newer one.
    /// </summary>
    private int _filterVersion;

    public BookingPageViewModel(
        IBookingQueryService queryService,
        IBookingCommandService commandService,
        IBookingWorkflowService workflowService,
        IDialogService dialogService,
        ILogger<BookingPageViewModel>? logger = null,
        ILoggerFactory? loggerFactory = null)
    {
        _queryService = queryService;
        _commandService = commandService;
        _workflowService = workflowService;
        _dialogService = dialogService;
        _logger = logger ?? NullLogger<BookingPageViewModel>.Instance;
        _loggerFactory = loggerFactory;

        Bookings = new ObservableCollection<BookingDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        OpenWizardCommand = new RelayCommand(_ => OpenWizard());
        CreateBookingCommand = new AsyncRelayCommand(
            _ => CreateBookingAsync(),
            _ => !string.IsNullOrWhiteSpace(NewBookingCustomerName)
                && !string.IsNullOrWhiteSpace(NewBookingServiceName)
                && NewBookingDate.HasValue);
        ConfirmBookingCommand = new AsyncRelayCommand(
            _ => ChangeStatusAsync(BookingStatus.Confirmed),
            _ => SelectedBooking?.Status == BookingStatus.Pending);
        // Owner App Booking Integration: gated on SupportsInProgressAndNoShowStatuses (see
        // IBookingCommandService's own doc comment) - a backend-connected command service
        // reports false here, since ROJAN_Backend's BookingStatus has no InProgress/NoShow
        // equivalent, so these two actions are disabled rather than left to fail at the
        // repository call.
        StartBookingCommand = new AsyncRelayCommand(
            _ => ChangeStatusAsync(BookingStatus.InProgress),
            _ => _commandService.SupportsInProgressAndNoShowStatuses
                && SelectedBooking is { Status: BookingStatus.Pending or BookingStatus.Confirmed });
        // Completed is only reachable via InProgress (see BookingRules) - this CanExecute used to
        // read Status == Confirmed, which let the button call UpdateBookingStatusAsync with an
        // illegal Confirmed -> Completed transition and throw at runtime (Sprint 3 Commit 3 fix).
        CompleteBookingCommand = new AsyncRelayCommand(
            _ => ChangeStatusAsync(BookingStatus.Completed),
            _ => SelectedBooking?.Status == BookingStatus.InProgress);
        NoShowBookingCommand = new AsyncRelayCommand(
            _ => ChangeStatusAsync(BookingStatus.NoShow),
            _ => _commandService.SupportsInProgressAndNoShowStatuses
                && SelectedBooking?.Status == BookingStatus.Confirmed);
        CancelBookingCommand = new AsyncRelayCommand(
            _ => CancelSelectedBookingAsync(),
            _ => SelectedBooking is { Status: BookingStatus.Pending or BookingStatus.Confirmed });
        RescheduleBookingCommand = new AsyncRelayCommand(
            _ => RescheduleSelectedBookingAsync(),
            _ => RescheduleDate.HasValue
                && SelectedBooking is { Status: BookingStatus.Pending or BookingStatus.Confirmed or BookingStatus.InProgress });

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, so there is nothing
        // left that could become an unobserved task exception.
        _ = LoadAsync();
    }

    public ObservableCollection<BookingDto> Bookings { get; }

    public IReadOnlyList<int> AvailableDurations { get; } = [30, 45, 60, 90, 120];

    /// <summary>Re-runs the load - bound as the Retry action on DashboardWidget's Error state.</summary>
    public ICommand LoadCommand { get; }

    /// <summary>Opens the Booking Wizard dialog - real Customer/Service/Specialist/Calendar-backed booking creation, distinct from the free-text quick-add form on this same page.</summary>
    public ICommand OpenWizardCommand { get; }

    public ICommand CreateBookingCommand { get; }

    public ICommand ConfirmBookingCommand { get; }

    /// <summary>Pending or Confirmed -&gt; InProgress, per <see cref="Rojan.Desktop.Domain.Bookings.BookingRules"/>.</summary>
    public ICommand StartBookingCommand { get; }

    public ICommand CompleteBookingCommand { get; }

    /// <summary>Confirmed -&gt; NoShow, per <see cref="Rojan.Desktop.Domain.Bookings.BookingRules"/>.</summary>
    public ICommand NoShowBookingCommand { get; }

    public ICommand CancelBookingCommand { get; }

    /// <summary>Moves the selected booking to <see cref="RescheduleDate"/> (same time-of-day, same specialist) via <see cref="IBookingWorkflowService.RescheduleBookingAsync"/> - never the plain command service directly, so the Calendar release/reserve orchestration always runs.</summary>
    public ICommand RescheduleBookingCommand { get; }

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

    /// <summary>Page Stability: true while an already-displayed booking list is reloaded in place (the list stays visible) - see <see cref="DashboardStateExtensions"/>.</summary>
    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set => SetProperty(ref _isRefreshing, value);
    }

    /// <summary>Page Stability: inline, non-destructive failure message for the selected booking's status/cancel/reschedule actions - never a page-wide Error state.</summary>
    public string? ActionErrorMessage
    {
        get => _actionErrorMessage;
        private set
        {
            if (SetProperty(ref _actionErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasActionError));
            }
        }
    }

    public bool HasActionError => !string.IsNullOrEmpty(ActionErrorMessage);

    /// <summary>Page Stability: inline, non-destructive failure message for the quick-add New Booking form - the form's contents are kept for a retry.</summary>
    public string? CreateErrorMessage
    {
        get => _createErrorMessage;
        private set
        {
            if (SetProperty(ref _createErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasCreateError));
            }
        }
    }

    public bool HasCreateError => !string.IsNullOrEmpty(CreateErrorMessage);

    public BookingDto? SelectedBooking
    {
        get => _selectedBooking;
        set
        {
            // Page Stability: Bookings.Clear() inside ReplaceBookings makes the bound ListBox push
            // null back here - that is the collection being rebuilt, not the user deselecting, so it
            // must not drop the selection ReplaceBookings is about to restore by Id.
            if (value is null && _isReplacingBookings)
            {
                return;
            }

            var previousId = _selectedBooking?.Id;
            if (SetProperty(ref _selectedBooking, value) && value?.Id != previousId)
            {
                // A message about a different booking's failed action no longer applies.
                ActionErrorMessage = null;
            }
        }
    }

    public string NewBookingCustomerName
    {
        get => _newBookingCustomerName;
        set => SetProperty(ref _newBookingCustomerName, value);
    }

    public string NewBookingServiceName
    {
        get => _newBookingServiceName;
        set => SetProperty(ref _newBookingServiceName, value);
    }

    public string NewBookingSpecialistName
    {
        get => _newBookingSpecialistName;
        set => SetProperty(ref _newBookingSpecialistName, value);
    }

    /// <summary>Date only - time-of-day defaults to a fixed 10:00 AM slot (Phase 11 foundation simplification, no time picker yet).</summary>
    public DateTime? NewBookingDate
    {
        get => _newBookingDate;
        set => SetProperty(ref _newBookingDate, value);
    }

    public int NewBookingDurationMinutes
    {
        get => _newBookingDurationMinutes;
        set => SetProperty(ref _newBookingDurationMinutes, value);
    }

    /// <summary>The selected booking's target date for <see cref="RescheduleBookingCommand"/> - date only, same "no time picker yet" simplification <see cref="NewBookingDate"/> already uses; the booking's existing time-of-day is preserved, only the date moves.</summary>
    public DateTime? RescheduleDate
    {
        get => _rescheduleDate;
        set => SetProperty(ref _rescheduleDate, value);
    }

    /// <summary>Free text, matched against customer/service/specialist name and notes - the "search over relevant booking fields" requirement.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public string CustomerNameFilter
    {
        get => _customerNameFilter;
        set
        {
            if (SetProperty(ref _customerNameFilter, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public string ServiceNameFilter
    {
        get => _serviceNameFilter;
        set
        {
            if (SetProperty(ref _serviceNameFilter, value))
            {
                _ = LoadAsync();
            }
        }
    }

    /// <summary>Null means "every status" - the first entry of <see cref="StatusFilterOptions"/>.</summary>
    public BookingStatus? StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public DateTime? DateFromFilter
    {
        get => _dateFromFilter;
        set
        {
            if (SetProperty(ref _dateFromFilter, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public DateTime? DateToFilter
    {
        get => _dateToFilter;
        set
        {
            if (SetProperty(ref _dateToFilter, value))
            {
                _ = LoadAsync();
            }
        }
    }

    /// <summary>Bindable options for the status filter ComboBox - leads with <c>null</c> ("every status") followed by every real <see cref="BookingStatus"/> value.</summary>
    public IReadOnlyList<BookingStatus?> StatusFilterOptions { get; } =
        new BookingStatus?[] { null }.Concat(Enum.GetValues<BookingStatus>().Cast<BookingStatus?>()).ToList();

    private async Task LoadAsync()
    {
        // Page Stability: only a first load (or a retry from Error) hides the list behind Loading -
        // reloading a list that is already on screen is a refresh, see DashboardStateExtensions.
        var isRefresh = State.HasSettledResult();
        ErrorMessage = null;
        if (isRefresh)
        {
            IsRefreshing = true;
        }
        else
        {
            State = DashboardState.Loading;
        }

        var requestVersion = ++_filterVersion;

        try
        {
            var bookings = await _queryService.SearchBookingsAsync(BuildFilter()).ConfigureAwait(true);

            if (requestVersion != _filterVersion)
            {
                // A newer filter change (or another reload) started after
                // this one - its result will win instead, so applying this
                // now-stale response would flash outdated data.
                return;
            }

            ReplaceBookings(bookings);

            State = Bookings.Count == 0
                ? DashboardState.Empty
                : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page ViewModel in this app.
        catch (Exception)
#pragma warning restore CA1031
        {
            if (requestVersion == _filterVersion)
            {
                // A failed refresh keeps the still-valid list on screen; DashboardWidget shows
                // ErrorMessage inline (with Retry) while State stays Loaded/Empty.
                ErrorMessage = Strings.Common_ActionFailedMessage;
                if (!isRefresh)
                {
                    State = DashboardState.Error;
                }

                LogOperationFailed(nameof(LoadAsync));
            }
        }
        finally
        {
            if (requestVersion == _filterVersion)
            {
                IsRefreshing = false;
            }
        }
    }

    /// <summary>
    /// Page Stability: rebuilds <see cref="Bookings"/> while keeping the selected booking selected by
    /// its stable Id (a reload returns new <see cref="BookingDto"/> instances). Falls back to the
    /// first row only when there was no selection, or the selected booking is no longer in the
    /// (filtered) result at all.
    /// </summary>
    private void ReplaceBookings(IReadOnlyList<BookingDto> bookings)
    {
        var selectedId = SelectedBooking?.Id;

        _isReplacingBookings = true;
        try
        {
            Bookings.Clear();
            foreach (var booking in bookings)
            {
                Bookings.Add(booking);
            }
        }
        finally
        {
            _isReplacingBookings = false;
        }

        var preserved = selectedId is null ? null : Bookings.FirstOrDefault(booking => booking.Id == selectedId);
        SelectedBooking = preserved ?? (Bookings.Count > 0 ? Bookings[0] : null);

        // Re-announce even when the preserved record compares equal to the old one: Clear() already
        // reset the bound ListBox's own SelectedItem, which only re-syncs on a change notification.
        OnPropertyChanged(nameof(SelectedBooking));
    }

    private BookingSearchFilter BuildFilter() => new(
        SearchText: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
        CustomerName: string.IsNullOrWhiteSpace(CustomerNameFilter) ? null : CustomerNameFilter,
        ServiceName: string.IsNullOrWhiteSpace(ServiceNameFilter) ? null : ServiceNameFilter,
        Status: StatusFilter,
        DateFrom: DateFromFilter.HasValue ? DateOnly.FromDateTime(DateFromFilter.Value) : null,
        DateTo: DateToFilter.HasValue ? DateOnly.FromDateTime(DateToFilter.Value) : null);

    private async Task CreateBookingAsync()
    {
        var scheduledAt = new DateTimeOffset(NewBookingDate!.Value.Year, NewBookingDate.Value.Month, NewBookingDate.Value.Day, 10, 0, 0, DateTimeOffset.Now.Offset);
        var request = new CreateBookingRequest(
            NewBookingCustomerName,
            NewBookingServiceName,
            NewBookingSpecialistName,
            scheduledAt,
            NewBookingDurationMinutes,
            string.Empty);

        BookingDto created;
        CreateErrorMessage = null;
        try
        {
            created = await _commandService.CreateBookingAsync(request).ConfigureAwait(true);
        }
#pragma warning disable CA1031 // Phase 7.4.4: top-level command boundary - any failure must surface inline (never crash, never the input the user just typed silently discarded) - same justified broad catch as LoadAsync's own boundary in this class.
        catch (Exception)
#pragma warning restore CA1031
        {
            // Deliberately does not clear the New Booking form fields here - a failed submission
            // should let the user retry with what they already typed, not lose it. Page Stability:
            // reported inline on the form, never as a page-wide Error that would hide the list.
            CreateErrorMessage = Strings.Common_ActionFailedMessage;
            LogOperationFailed(nameof(CreateBookingAsync));
            return;
        }

        NewBookingCustomerName = string.Empty;
        NewBookingServiceName = string.Empty;
        NewBookingSpecialistName = string.Empty;
        NewBookingDate = DateTime.Today.AddDays(1);
        NewBookingDurationMinutes = 60;

        // LoadAsync owns its own failure handling (a failed refresh keeps the list, reported inline).
        await LoadAsync().ConfigureAwait(true);
        SelectedBooking = Bookings.FirstOrDefault(booking => booking.Id == created.Id) ?? SelectedBooking;
    }

    private void OpenWizard()
    {
        var wizard = new BookingWizardViewModel(_workflowService, _dialogService, () => _ = LoadAsync(), _loggerFactory?.CreateLogger<BookingWizardViewModel>());
        _dialogService.ShowDialog(wizard);
    }

    private async Task ChangeStatusAsync(BookingStatus status)
    {
        if (SelectedBooking is null)
        {
            return;
        }

        var bookingId = SelectedBooking.Id;
        ActionErrorMessage = null;

        try
        {
            await _commandService.UpdateBookingStatusAsync(bookingId, status).ConfigureAwait(true);
        }
#pragma warning disable CA1031 // Phase 7.4.4: same justified broad catch as CreateBookingAsync's own boundary in this class.
        catch (Exception)
#pragma warning restore CA1031
        {
            // Page Stability: inline next to the actions - the list and selection stay as they were.
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            LogOperationFailed(nameof(ChangeStatusAsync));
            return;
        }

        await LoadAsync().ConfigureAwait(true);
        SelectedBooking = Bookings.FirstOrDefault(booking => booking.Id == bookingId);
    }

    /// <summary>
    /// Cancel goes through <see cref="IBookingWorkflowService.CancelBookingAsync"/>
    /// rather than <see cref="ChangeStatusAsync"/>/<see cref="IBookingCommandService.UpdateBookingStatusAsync"/>
    /// directly - the workflow service also releases the booking's reserved
    /// Calendar slot (when it has a real specialist id; free-text quick-add
    /// bookings never reserved one, so there is nothing to release). Using
    /// the plain status-only path here would leave a Wizard-created
    /// booking's slot stuck as Booked with no booking left to show for it.
    /// </summary>
    private async Task CancelSelectedBookingAsync()
    {
        if (SelectedBooking is null)
        {
            return;
        }

        var bookingId = SelectedBooking.Id;
        ActionErrorMessage = null;

        try
        {
            await _workflowService.CancelBookingAsync(bookingId).ConfigureAwait(true);
        }
#pragma warning disable CA1031 // Phase 7.4.4: same justified broad catch as CreateBookingAsync's own boundary in this class.
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            LogOperationFailed(nameof(CancelSelectedBookingAsync));
            return;
        }

        await LoadAsync().ConfigureAwait(true);
        SelectedBooking = Bookings.FirstOrDefault(booking => booking.Id == bookingId);
    }

    /// <summary>
    /// Goes through <see cref="IBookingWorkflowService.RescheduleBookingAsync"/> - never
    /// <see cref="IBookingCommandService"/> directly - for the same reason Cancel does: the
    /// workflow service also releases the old Calendar reservation and reserves the new one (when
    /// the booking has a real specialist id), which a direct command-service call would skip
    /// entirely.
    /// </summary>
    private async Task RescheduleSelectedBookingAsync()
    {
        if (SelectedBooking is null || !RescheduleDate.HasValue)
        {
            return;
        }

        var bookingId = SelectedBooking.Id;
        var originalTimeOfDay = SelectedBooking.ScheduledAt.TimeOfDay;
        var newScheduledAt = new DateTimeOffset(RescheduleDate.Value.Date + originalTimeOfDay, SelectedBooking.ScheduledAt.Offset);

        ActionErrorMessage = null;

        try
        {
            await _workflowService.RescheduleBookingAsync(bookingId, newScheduledAt).ConfigureAwait(true);
        }
#pragma warning disable CA1031 // Phase 7.4.4: same justified broad catch as CreateBookingAsync's own boundary in this class - deliberately does not clear RescheduleDate here, same "let the user retry" reasoning as CreateBookingAsync's own form fields.
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            LogOperationFailed(nameof(RescheduleSelectedBookingAsync));
            return;
        }

        RescheduleDate = null;
        await LoadAsync().ConfigureAwait(true);
        SelectedBooking = Bookings.FirstOrDefault(booking => booking.Id == bookingId);
    }

    // Operation name only: the caught exception is never passed to the logger
    // (Phase 8.15+ security rule - backend response bodies must not reach the log).
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Booking operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);
}
