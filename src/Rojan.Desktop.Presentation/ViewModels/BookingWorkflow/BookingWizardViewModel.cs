using System.Collections.ObjectModel;
using System.Windows.Input;
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
public sealed class BookingWizardViewModel : ViewModelBase
{
    private readonly IBookingWorkflowService _workflowService;
    private readonly IDialogService _dialogService;
    private readonly Action? _onBookingCreated;

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

    public BookingWizardViewModel(IBookingWorkflowService workflowService, IDialogService dialogService, Action? onBookingCreated = null)
    {
        _workflowService = workflowService;
        _dialogService = dialogService;
        _onBookingCreated = onBookingCreated;

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

            State = Customers.Count == 0 || Services.Count == 0 || Specialists.Count == 0
                ? DashboardState.Empty
                : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the dialog - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
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
    /// </summary>
    private void RefreshEligibleSpecialists()
    {
        EligibleSpecialists.Clear();

        if (SelectedService is not null)
        {
            foreach (var specialist in Specialists.Where(specialist => IsEligibleForSelectedService(specialist, SelectedService.Id)))
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
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
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
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the dialog - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
        }
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
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
        }
    }
}
