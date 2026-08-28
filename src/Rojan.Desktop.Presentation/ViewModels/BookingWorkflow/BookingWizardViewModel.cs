using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.BookingWorkflow;
using Rojan.Desktop.Presentation.Dialogs;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.BookingWorkflow;

/// <summary>
/// Drives BookingWizardView - a linear, step-by-step booking creation flow
/// shown in Shell's dialog region via <see cref="IDialogService"/>, unlike
/// every prior module's single-page list-plus-detail shape. Depends only
/// on <see cref="IBookingWorkflowService"/> (which itself coordinates
/// Customers/Services/Specialists/Calendar/Bookings) and
/// <see cref="IDialogService"/> - never reaches those other Application
/// services directly, keeping this ViewModel's own dependency surface
/// small despite the wizard touching five business domains.
/// </summary>
public sealed partial class BookingWizardViewModel : ViewModelBase
{
    private readonly IBookingWorkflowService _workflowService;
    private readonly IDialogService _dialogService;
    private readonly Action? _onBookingCreated;
    private readonly ILogger<BookingWizardViewModel> _logger;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private BookingWizardStep _currentStep = BookingWizardStep.Customer;

    private WorkflowCustomerOptionDto? _selectedCustomer;
    private WorkflowServiceOptionDto? _selectedService;
    private WorkflowSpecialistOptionDto? _selectedSpecialist;
    private bool _hasNoEligibleSpecialists;
    private string? _noEligibleSpecialistsMessage;
    private DateTime _selectedDate = DateTime.Today.AddDays(1);
    private WorkflowSlotDto? _selectedSlot;
    private string _notes = string.Empty;
    private BookingConfirmationDto? _confirmation;
    private string _guestFullName = string.Empty;
    private string _guestPhone = string.Empty;
    private bool _isAddingGuestCustomer;
    private bool _hasNoBookableData;
    private DateOnly? _suggestedNextAvailableDate;
    private bool _isSearchingNextAvailableDate;
    private CancellationTokenSource? _nextAvailableDateSearchCts;

    public BookingWizardViewModel(
        IBookingWorkflowService workflowService,
        IDialogService dialogService,
        Action? onBookingCreated = null,
        ILogger<BookingWizardViewModel>? logger = null)
    {
        _workflowService = workflowService;
        _dialogService = dialogService;
        _onBookingCreated = onBookingCreated;
        _logger = logger ?? NullLogger<BookingWizardViewModel>.Instance;

        Customers = new ObservableCollection<WorkflowCustomerOptionDto>();
        Services = new ObservableCollection<WorkflowServiceOptionDto>();
        Specialists = new ObservableCollection<WorkflowSpecialistOptionDto>();
        EligibleSpecialists = new ObservableCollection<WorkflowSpecialistOptionDto>();
        AvailableSlots = new ObservableCollection<WorkflowSlotDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadOptionsAsync());
        NextCommand = new AsyncRelayCommand(_ => NextAsync(), _ => CanGoNext());
        BackCommand = new RelayCommand(_ => Back(), _ => CurrentStep is not (BookingWizardStep.Customer or BookingWizardStep.Confirmation));
        CancelCommand = new RelayCommand(_ => _dialogService.CloseDialog());
        ConfirmBookingCommand = new AsyncRelayCommand(_ => ConfirmBookingAsync(), _ => CurrentStep == BookingWizardStep.Review);
        DoneCommand = new RelayCommand(_ => _dialogService.CloseDialog());
        AddGuestCustomerCommand = new AsyncRelayCommand(_ => AddGuestCustomerAsync(), _ => CanAddGuestCustomer());
        TryNextAvailableDateCommand = new AsyncRelayCommand(_ => TryNextAvailableDateAsync(), _ => SuggestedNextAvailableDate is not null);

        // Safe fire-and-forget: LoadOptionsAsync catches every failure
        // internally and represents it via State/ErrorMessage, same
        // reasoning as every other page ViewModel's constructor-time load.
        _ = LoadOptionsAsync();
    }

    public ObservableCollection<WorkflowCustomerOptionDto> Customers { get; }

    public ObservableCollection<WorkflowServiceOptionDto> Services { get; }

    public ObservableCollection<WorkflowSpecialistOptionDto> Specialists { get; }

    /// <summary>
    /// Booking Eligibility Filter: the Specialist step's picker binds to
    /// this, not <see cref="Specialists"/> directly - every specialist
    /// eligible for <see cref="SelectedService"/>, per ROJAN_Backend's own
    /// eligibility rule (<see cref="WorkflowSpecialistOptionDto.AssignedServiceIds"/>
    /// empty means unrestricted). Recomputed by <see cref="RefreshEligibleSpecialists"/>
    /// whenever <see cref="SelectedService"/> changes or the option list
    /// reloads - a UX filter only, never a substitute for ROJAN_Backend's
    /// own authoritative check (still enforced, unchanged, inside the
    /// availability/booking-creation endpoints this wizard already calls).
    /// </summary>
    public ObservableCollection<WorkflowSpecialistOptionDto> EligibleSpecialists { get; }

    public ObservableCollection<WorkflowSlotDto> AvailableSlots { get; }

    public ICommand LoadCommand { get; }

    public ICommand NextCommand { get; }

    public ICommand BackCommand { get; }

    public ICommand CancelCommand { get; }

    public ICommand ConfirmBookingCommand { get; }

    public ICommand DoneCommand { get; }

    public ICommand AddGuestCustomerCommand { get; }

    /// <summary>Booking Intelligence Phase 1: accepts <see cref="SuggestedNextAvailableDate"/> - sets <see cref="SelectedDate"/> to it and reloads <see cref="AvailableSlots"/> for that date. A single explicit user click; never auto-applied.</summary>
    public ICommand TryNextAvailableDateCommand { get; }

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

    public BookingWizardStep CurrentStep
    {
        get => _currentStep;
        private set => SetProperty(ref _currentStep, value);
    }

    public WorkflowCustomerOptionDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetProperty(ref _selectedCustomer, value);
    }

    /// <summary>Reception Stabilization Sprint: the Customer step's "Walk-in" entry point - bound to a text field, submitted via <see cref="AddGuestCustomerCommand"/>.</summary>
    public string GuestFullName
    {
        get => _guestFullName;
        set => SetProperty(ref _guestFullName, value);
    }

    public string GuestPhone
    {
        get => _guestPhone;
        set => SetProperty(ref _guestPhone, value);
    }

    public bool IsAddingGuestCustomer
    {
        get => _isAddingGuestCustomer;
        private set => SetProperty(ref _isAddingGuestCustomer, value);
    }

    /// <summary>
    /// Booking Intelligence Phase 1: true when <see cref="LoadOptionsAsync"/>
    /// found no Customers, Services, or Specialists at all - a distinct,
    /// dedicated signal from the shared <see cref="State"/> so it cannot be
    /// spuriously re-triggered by an unrelated later Empty state (e.g. the
    /// TimeSlot step finding no slots on a given day). Backs the Customer
    /// step's own "nothing to book yet" empty-state message.
    /// </summary>
    public bool HasNoBookableData
    {
        get => _hasNoBookableData;
        private set => SetProperty(ref _hasNoBookableData, value);
    }

    public WorkflowServiceOptionDto? SelectedService
    {
        get => _selectedService;
        set
        {
            if (SetProperty(ref _selectedService, value))
            {
                RefreshEligibleSpecialists();
            }
        }
    }

    public WorkflowSpecialistOptionDto? SelectedSpecialist
    {
        get => _selectedSpecialist;
        set => SetProperty(ref _selectedSpecialist, value);
    }

    /// <summary>Booking Eligibility Filter: true when <see cref="SelectedService"/> is set but no specialist is eligible for it - backs the empty-state message's visibility.</summary>
    public bool HasNoEligibleSpecialists
    {
        get => _hasNoEligibleSpecialists;
        private set => SetProperty(ref _hasNoEligibleSpecialists, value);
    }

    /// <summary>Non-destructive, never a dead end: explains why the Specialist step's picker is empty rather than leaving Reception guessing (same reasoning as <c>Specialists.SpecialistProfileViewModel</c>'s own inline error messages).</summary>
    public string? NoEligibleSpecialistsMessage
    {
        get => _noEligibleSpecialistsMessage;
        private set => SetProperty(ref _noEligibleSpecialistsMessage, value);
    }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set => SetProperty(ref _selectedDate, value);
    }

    public WorkflowSlotDto? SelectedSlot
    {
        get => _selectedSlot;
        set => SetProperty(ref _selectedSlot, value);
    }

    /// <summary>
    /// Booking Intelligence Phase 1: the first date, strictly after
    /// <see cref="SelectedDate"/>, within a small bounded window that
    /// <see cref="SearchNextAvailableDateAsync"/> found at least one open
    /// slot for - null while unsearched, while searching, or when the
    /// window was exhausted with nothing found. This is Backend's own
    /// answer, read <see cref="NextAvailableDateSearchWindowDays"/> times
    /// at most via the same <see cref="IBookingWorkflowService.GetAvailableSlotsAsync"/>
    /// call already used for the originally-picked date - never a locally
    /// computed or inferred slot.
    /// </summary>
    public DateOnly? SuggestedNextAvailableDate
    {
        get => _suggestedNextAvailableDate;
        private set
        {
            if (SetProperty(ref _suggestedNextAvailableDate, value))
            {
                OnPropertyChanged(nameof(HasSuggestedNextAvailableDate));
            }
        }
    }

    public bool HasSuggestedNextAvailableDate => SuggestedNextAvailableDate is not null;

    /// <summary>Backs a "looking for the next available day..." caption - a distinct signal from <see cref="State"/> so it can be shown alongside the existing empty-slots message rather than replacing it.</summary>
    public bool IsSearchingNextAvailableDate
    {
        get => _isSearchingNextAvailableDate;
        private set => SetProperty(ref _isSearchingNextAvailableDate, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public BookingConfirmationDto? Confirmation
    {
        get => _confirmation;
        private set => SetProperty(ref _confirmation, value);
    }

    private async Task LoadOptionsAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var options = await _workflowService.GetBookingOptionsAsync().ConfigureAwait(true);

            Customers.Clear();
            foreach (var customer in options.Customers)
            {
                Customers.Add(customer);
            }

            Services.Clear();
            foreach (var service in options.Services)
            {
                Services.Add(service);
            }

            Specialists.Clear();
            foreach (var specialist in options.Specialists)
            {
                Specialists.Add(specialist);
            }

            RefreshEligibleSpecialists();

            HasNoBookableData = Customers.Count == 0 || Services.Count == 0 || Specialists.Count == 0;
            State = HasNoBookableData ? DashboardState.Empty : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the dialog - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = ToFriendlyErrorMessage(exception);
            State = DashboardState.Error;
            LogOperationFailed(nameof(LoadOptionsAsync));
        }
    }

    /// <summary>
    /// Booking Eligibility Filter: recomputes <see cref="EligibleSpecialists"/>
    /// as every entry in <see cref="Specialists"/> eligible for
    /// <see cref="SelectedService"/>, mirroring ROJAN_Backend's own
    /// <c>isSpecialistEligibleForService</c> rule exactly - an empty
    /// <see cref="WorkflowSpecialistOptionDto.AssignedServiceIds"/> means
    /// unrestricted (eligible for everything), not "eligible for nothing".
    /// If <see cref="SelectedSpecialist"/> is no longer eligible after this
    /// recompute (e.g. the user went back and changed <see cref="SelectedService"/>),
    /// it is cleared - the wizard must never let a since-invalidated pair
    /// silently ride through to Date/TimeSlot. This is a UX filter only;
    /// ROJAN_Backend's own check inside the availability/booking-creation
    /// endpoints remains the sole authority and is unaffected by anything
    /// here.
    ///
    /// Booking Intelligence Phase 1 (Smart Specialist Ordering):
    /// <see cref="EligibleSpecialists"/> is additionally sorted -
    /// specialists explicitly assigned to <see cref="SelectedService"/>
    /// (<see cref="IsExplicitlyAssignedToSelectedService"/>) first, every
    /// other eligible (unassigned/generalist) specialist after, each group
    /// alphabetical by <see cref="WorkflowSpecialistOptionDto.FullName"/>.
    /// This is a fixed, explainable rule over data already fetched (the
    /// same <see cref="WorkflowSpecialistOptionDto.AssignedServiceIds"/>
    /// the eligibility filter itself uses) - no scoring, no weighting, no
    /// inferred/learned ranking, not an AI recommendation. Unassigned
    /// specialists are never removed or hidden by this - they remain fully
    /// eligible and pickable, only ordered after the explicit matches.
    /// </summary>
    private void RefreshEligibleSpecialists()
    {
        EligibleSpecialists.Clear();

        if (SelectedService is not null)
        {
            var ordered = Specialists
                .Where(specialist => IsEligibleForSelectedService(specialist, SelectedService.Id))
                .OrderByDescending(specialist => IsExplicitlyAssignedToSelectedService(specialist, SelectedService.Id))
                .ThenBy(specialist => specialist.FullName, StringComparer.Ordinal);

            foreach (var specialist in ordered)
            {
                EligibleSpecialists.Add(specialist);
            }
        }

        if (SelectedSpecialist is not null && !EligibleSpecialists.Contains(SelectedSpecialist))
        {
            SelectedSpecialist = null;
        }

        HasNoEligibleSpecialists = SelectedService is not null && EligibleSpecialists.Count == 0;
        NoEligibleSpecialistsMessage = HasNoEligibleSpecialists ? Strings.BookingWizard_NoEligibleSpecialistsMessage : null;
    }

    private static bool IsEligibleForSelectedService(WorkflowSpecialistOptionDto specialist, string serviceId) =>
        specialist.AssignedServiceIds.Count == 0 || specialist.AssignedServiceIds.Contains(serviceId);

    /// <summary>Booking Intelligence Phase 1 (Smart Specialist Ordering): true only for a specialist with a real, explicit assignment to <paramref name="serviceId"/> - distinct from the broader eligibility check above, which also admits unrestricted/generalist specialists with an empty <see cref="WorkflowSpecialistOptionDto.AssignedServiceIds"/>.</summary>
    private static bool IsExplicitlyAssignedToSelectedService(WorkflowSpecialistOptionDto specialist, string serviceId) =>
        specialist.AssignedServiceIds.Count > 0 && specialist.AssignedServiceIds.Contains(serviceId);

    private bool CanGoNext() => CurrentStep switch
    {
        BookingWizardStep.Customer => SelectedCustomer is not null,
        BookingWizardStep.Service => SelectedService is not null,
        BookingWizardStep.Specialist => SelectedSpecialist is not null,
        BookingWizardStep.Date => true,
        BookingWizardStep.TimeSlot => SelectedSlot is not null,
        _ => false,
    };

    private bool CanAddGuestCustomer() => !IsAddingGuestCustomer && !string.IsNullOrWhiteSpace(GuestFullName);

    /// <summary>Reception Stabilization Sprint: the Customer step's "Walk-in" entry point - creates a new CRM customer via <see cref="IBookingWorkflowService.CreateGuestCustomerAsync"/> (a real backend write, gated the same as any other customer creation), adds it to <see cref="Customers"/>, and selects it, exactly as if it had been picked from the list. The created customer is never bookable immediately (<see cref="WorkflowCustomerOptionDto.IsLinkedToAccount"/> is always <see langword="false"/> for a freshly-created guest) - <see cref="ConfirmBookingAsync"/> is what surfaces that clearly, not this method.</summary>
    private async Task AddGuestCustomerAsync()
    {
        IsAddingGuestCustomer = true;
        ErrorMessage = null;

        try
        {
            var guest = await _workflowService.CreateGuestCustomerAsync(GuestFullName.Trim(), GuestPhone.Trim()).ConfigureAwait(true);
            Customers.Add(guest);
            SelectedCustomer = guest;
            GuestFullName = string.Empty;
            GuestPhone = string.Empty;

            // Booking Intelligence Phase 1: a wizard opened against a salon with services/specialists
            // but zero customers could only reach this command from the Customer step's own
            // HasNoBookableData empty-state - re-check now in case this guest was the missing piece.
            HasNoBookableData = Customers.Count == 0 || Services.Count == 0 || Specialists.Count == 0;
            State = DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level command boundary: any failure must surface via ErrorMessage, not crash the dialog - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            // State = Error only drives the shared error banner's visibility (BookingWizardView's
            // Row 2) and the TimeSlot step's empty-state card - none of the 7 step panels
            // (including this one, Customer) gate their own visibility on State, so this does not
            // hide the picker the way it would on a full-page ViewModel.
            ErrorMessage = ToFriendlyErrorMessage(exception);
            State = DashboardState.Error;
            LogOperationFailed(nameof(AddGuestCustomerAsync));
        }
        finally
        {
            IsAddingGuestCustomer = false;
        }
    }

    private async Task NextAsync()
    {
        switch (CurrentStep)
        {
            case BookingWizardStep.Customer:
                CurrentStep = BookingWizardStep.Service;
                break;
            case BookingWizardStep.Service:
                CurrentStep = BookingWizardStep.Specialist;
                break;
            case BookingWizardStep.Specialist:
                CurrentStep = BookingWizardStep.Date;
                break;
            case BookingWizardStep.Date:
                await LoadAvailableSlotsAsync().ConfigureAwait(true);
                CurrentStep = BookingWizardStep.TimeSlot;
                break;
            case BookingWizardStep.TimeSlot:
                CurrentStep = BookingWizardStep.Review;
                break;
        }
    }

    private void Back()
    {
        CurrentStep = CurrentStep switch
        {
            BookingWizardStep.Service => BookingWizardStep.Customer,
            BookingWizardStep.Specialist => BookingWizardStep.Service,
            BookingWizardStep.Date => BookingWizardStep.Specialist,
            BookingWizardStep.TimeSlot => BookingWizardStep.Date,
            BookingWizardStep.Review => BookingWizardStep.TimeSlot,
            _ => CurrentStep,
        };
    }

    private async Task LoadAvailableSlotsAsync()
    {
        if (SelectedSpecialist is null || SelectedService is null)
        {
            return;
        }

        // Booking Intelligence Phase 1: a new date load obsoletes any in-flight next-available-date
        // probe from a previous empty result - cancel it before starting this one.
        CancelNextAvailableDateSearch();
        SuggestedNextAvailableDate = null;

        State = DashboardState.Loading;
        ErrorMessage = null;
        SelectedSlot = null;

        try
        {
            var slots = await _workflowService
                .GetAvailableSlotsAsync(SelectedSpecialist.Id, SelectedService.Id, DateOnly.FromDateTime(SelectedDate))
                .ConfigureAwait(true);

            AvailableSlots.Clear();
            foreach (var slot in slots)
            {
                AvailableSlots.Add(slot);
            }

            State = AvailableSlots.Count == 0 ? DashboardState.Empty : DashboardState.Loaded;

            if (State == DashboardState.Empty)
            {
                // Booking Intelligence Phase 1 (Smart Availability Presentation): safe fire-and-forget -
                // SearchNextAvailableDateAsync catches every failure internally (best-effort, never
                // surfaces as the page-level ErrorMessage) and is cancellable via _nextAvailableDateSearchCts.
                _ = SearchNextAvailableDateAsync();
            }
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the dialog - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = ToFriendlyErrorMessage(exception);
            State = DashboardState.Error;
            LogOperationFailed(nameof(LoadAvailableSlotsAsync));
        }
    }

    /// <summary>
    /// Booking Intelligence Phase 1 (Smart Availability Presentation): when
    /// <see cref="SelectedDate"/> has no slots, probes forward a small,
    /// fixed, bounded window (<see cref="NextAvailableDateSearchWindowDays"/>
    /// days) by calling the exact same, already-existing, Backend-authoritative
    /// <see cref="IBookingWorkflowService.GetAvailableSlotsAsync"/> once per
    /// candidate day, stopping at the first day with at least one slot.
    /// Nothing is computed, reserved, or decided client-side - every answer
    /// is Backend's own. Cancellable (a new date pick or a fresh probe
    /// supersedes an in-flight one via <see cref="CancelNextAvailableDateSearch"/>);
    /// any failure (including cancellation) is swallowed - this is a
    /// best-effort suggestion, never a substitute for the primary
    /// <see cref="ErrorMessage"/>/<see cref="State"/> the failed original
    /// load already surfaced.
    /// </summary>
    private async Task SearchNextAvailableDateAsync()
    {
        if (SelectedSpecialist is null || SelectedService is null)
        {
            return;
        }

        var cts = new CancellationTokenSource();
        _nextAvailableDateSearchCts = cts;

        IsSearchingNextAvailableDate = true;

        try
        {
            for (var offset = 1; offset <= NextAvailableDateSearchWindowDays; offset++)
            {
                cts.Token.ThrowIfCancellationRequested();

                var candidateDate = DateOnly.FromDateTime(SelectedDate).AddDays(offset);
                var slots = await _workflowService
                    .GetAvailableSlotsAsync(SelectedSpecialist.Id, SelectedService.Id, candidateDate, cts.Token)
                    .ConfigureAwait(true);

                if (slots.Count > 0)
                {
                    SuggestedNextAvailableDate = candidateDate;
                    return;
                }
            }
        }
#pragma warning disable CA1031 // Best-effort, swallowed by design - see this method's own doc comment. A stale/cancelled probe must never surface as the page-level ErrorMessage.
        catch (Exception)
        {
            // Swallowed by design - see this method's own doc comment.
        }
#pragma warning restore CA1031
        finally
        {
            if (ReferenceEquals(_nextAvailableDateSearchCts, cts))
            {
                IsSearchingNextAvailableDate = false;
            }
        }
    }

    private void CancelNextAvailableDateSearch()
    {
        _nextAvailableDateSearchCts?.Cancel();
        _nextAvailableDateSearchCts?.Dispose();
        _nextAvailableDateSearchCts = null;
        IsSearchingNextAvailableDate = false;
    }

    /// <summary>Booking Intelligence Phase 1: how many days forward <see cref="SearchNextAvailableDateAsync"/> probes before giving up - deliberately small and fixed, to bound the extra Backend load a single empty-date event can cause.</summary>
    private const int NextAvailableDateSearchWindowDays = 7;

    /// <summary>Booking Intelligence Phase 1: accepts the suggestion found by <see cref="SearchNextAvailableDateAsync"/> - moves <see cref="SelectedDate"/> to it and reloads, exactly as if the user had picked that date themselves.</summary>
    private async Task TryNextAvailableDateAsync()
    {
        if (SuggestedNextAvailableDate is null)
        {
            return;
        }

        SelectedDate = SuggestedNextAvailableDate.Value.ToDateTime(TimeOnly.MinValue);
        await LoadAvailableSlotsAsync().ConfigureAwait(true);
    }

    private async Task ConfirmBookingAsync()
    {
        if (SelectedCustomer is null || SelectedService is null || SelectedSpecialist is null || SelectedSlot is null)
        {
            return;
        }

        // Reception Stabilization Sprint: a customer with no linked backend user account will
        // always fail booking creation with a 409 CUSTOMER_NOT_LINKED_TO_ACCOUNT (a real,
        // unchangeable backend business rule) - surfaced here, clearly, at the last step, instead
        // of letting Reception hit that raw exception message after already completing all 7
        // steps.
        if (!SelectedCustomer.IsLinkedToAccount)
        {
            ErrorMessage = Strings.BookingWizard_CustomerNotLinkedToAccountMessage;
            State = DashboardState.Error;
            return;
        }

        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var request = new CreateBookingWorkflowRequest(
                SelectedCustomer.Id,
                SelectedCustomer.FullName,
                SelectedService.Id,
                SelectedService.Name,
                SelectedService.DurationMinutes,
                SelectedService.Price,
                SelectedSpecialist.Id,
                SelectedSpecialist.FullName,
                SelectedSlot.Start,
                Notes);

            Confirmation = await _workflowService.CreateBookingAsync(request).ConfigureAwait(true);
            CurrentStep = BookingWizardStep.Confirmation;
            State = DashboardState.Loaded;
            _onBookingCreated?.Invoke();
        }
#pragma warning disable CA1031 // Top-level command boundary: any failure must surface as the Error state, not crash the dialog - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = ToFriendlyErrorMessage(exception);
            State = DashboardState.Error;
            LogOperationFailed(nameof(ConfirmBookingAsync));
        }
    }

    /// <summary>
    /// Booking Intelligence Phase 1 (Booking Error Handling): maps a caught
    /// exception to a fixed, friendly, localized message - never the raw
    /// <see cref="Exception.Message"/> - the same typed-exception approach
    /// already established by <c>Security.LoginViewModel.SignInAsync</c>
    /// (separate catch blocks there; a switch expression here, since this
    /// same mapping is needed at four call sites in this class). A specific
    /// message for <see cref="ApiTimeoutException"/>/<see cref="ApiConnectivityException"/>
    /// (both mean "could not reach ROJAN_Backend"), a generic one for any
    /// other <see cref="ApiException"/> (a real backend rejection, e.g. an
    /// authoritative conflict check failing - see <see cref="ConfirmBookingAsync"/>'s
    /// own comment on ADR-004), and the same generic message as a last
    /// resort for anything unexpected - this class must never show internal
    /// exception detail to Reception.
    /// </summary>
    private static string ToFriendlyErrorMessage(Exception exception) => exception switch
    {
        ApiTimeoutException or ApiConnectivityException => Strings.BookingWizard_Error_Network,
        _ => Strings.BookingWizard_Error_Generic,
    };

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Booking wizard operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);
}
