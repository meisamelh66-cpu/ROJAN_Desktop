using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Bookings;
using Rojan.Desktop.Application.BookingWorkflow;
using Rojan.Desktop.Presentation.Dialogs;
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
/// </summary>
public sealed class BookingPageViewModel : ViewModelBase
{
    private readonly IBookingQueryService _queryService;
    private readonly IBookingCommandService _commandService;
    private readonly IBookingWorkflowService _workflowService;
    private readonly IDialogService _dialogService;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private BookingDto? _selectedBooking;
    private string _newBookingCustomerName = string.Empty;
    private string _newBookingServiceName = string.Empty;
    private string _newBookingSpecialistName = string.Empty;
    private DateTime? _newBookingDate = DateTime.Today.AddDays(1);
    private int _newBookingDurationMinutes = 60;

    public BookingPageViewModel(
        IBookingQueryService queryService,
        IBookingCommandService commandService,
        IBookingWorkflowService workflowService,
        IDialogService dialogService)
    {
        _queryService = queryService;
        _commandService = commandService;
        _workflowService = workflowService;
        _dialogService = dialogService;

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
        CompleteBookingCommand = new AsyncRelayCommand(
            _ => ChangeStatusAsync(BookingStatus.Completed),
            _ => SelectedBooking?.Status == BookingStatus.Confirmed);
        CancelBookingCommand = new AsyncRelayCommand(
            _ => CancelSelectedBookingAsync(),
            _ => SelectedBooking is { Status: BookingStatus.Pending or BookingStatus.Confirmed });

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

    public ICommand CompleteBookingCommand { get; }

    public ICommand CancelBookingCommand { get; }

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

    public BookingDto? SelectedBooking
    {
        get => _selectedBooking;
        set => SetProperty(ref _selectedBooking, value);
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

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var bookings = await _queryService.GetBookingsAsync().ConfigureAwait(true);

            Bookings.Clear();
            foreach (var booking in bookings)
            {
                Bookings.Add(booking);
            }

            if (SelectedBooking is null || Bookings.All(booking => booking.Id != SelectedBooking.Id))
            {
                SelectedBooking = Bookings.Count > 0 ? Bookings[0] : null;
            }
            else
            {
                SelectedBooking = Bookings.First(booking => booking.Id == SelectedBooking.Id);
            }

            State = Bookings.Count == 0
                ? DashboardState.Empty
                : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
        }
    }

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

        var created = await _commandService.CreateBookingAsync(request).ConfigureAwait(true);

        NewBookingCustomerName = string.Empty;
        NewBookingServiceName = string.Empty;
        NewBookingSpecialistName = string.Empty;
        NewBookingDate = DateTime.Today.AddDays(1);
        NewBookingDurationMinutes = 60;

        await LoadAsync().ConfigureAwait(true);
        SelectedBooking = Bookings.FirstOrDefault(booking => booking.Id == created.Id);
    }

    private void OpenWizard()
    {
        var wizard = new BookingWizardViewModel(_workflowService, _dialogService, () => _ = LoadAsync());
        _dialogService.ShowDialog(wizard);
    }

    private async Task ChangeStatusAsync(BookingStatus status)
    {
        if (SelectedBooking is null)
        {
            return;
        }

        var bookingId = SelectedBooking.Id;
        await _commandService.UpdateBookingStatusAsync(bookingId, status).ConfigureAwait(true);
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
        await _workflowService.CancelBookingAsync(bookingId).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
        SelectedBooking = Bookings.FirstOrDefault(booking => booking.Id == bookingId);
    }
}
