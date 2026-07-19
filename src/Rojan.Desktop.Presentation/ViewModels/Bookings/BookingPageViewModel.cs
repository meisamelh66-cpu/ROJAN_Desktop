using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Bookings;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Bookings;

/// <summary>
/// Drives BookingPage - the booking list on the left, a new-booking form,
/// and the selected booking's details plus status-transition actions on
/// the right. Depends only on Application services
/// (<see cref="IBookingQueryService"/>, <see cref="IBookingCommandService"/>),
/// consistent with Presentation never reaching past Application into
/// Domain/Infrastructure. Reuses <see cref="DashboardState"/> rather than a
/// duplicate enum, same reasoning as every other page ViewModel in this app.
/// Foundation scope (Phase 11): no calendar view, no conflict detection,
/// no cross-slice link to a real Customer record (CustomerName is free
/// text) - a working vertical slice end to end, not the full booking
/// management experience.
/// </summary>
public sealed class BookingPageViewModel : ViewModelBase
{
    private readonly IBookingQueryService _queryService;
    private readonly IBookingCommandService _commandService;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private BookingDto? _selectedBooking;
    private string _newBookingCustomerName = string.Empty;
    private string _newBookingServiceName = string.Empty;
    private string _newBookingSpecialistName = string.Empty;
    private DateTime? _newBookingDate = DateTime.Today.AddDays(1);
    private int _newBookingDurationMinutes = 60;

    public BookingPageViewModel(IBookingQueryService queryService, IBookingCommandService commandService)
    {
        _queryService = queryService;
        _commandService = commandService;

        Bookings = new ObservableCollection<BookingDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
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
            _ => ChangeStatusAsync(BookingStatus.Cancelled),
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
}
