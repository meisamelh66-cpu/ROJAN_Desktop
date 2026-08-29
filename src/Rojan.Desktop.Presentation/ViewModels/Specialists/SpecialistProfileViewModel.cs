using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Intelligence;
using Rojan.Desktop.Application.Services;
using Rojan.Desktop.Application.Specialists;
using Rojan.Desktop.Application.Specialists.Schedule;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Specialists;

/// <summary>
/// Drives the specialist profile panel for one selected specialist -
/// status display/edit, skills, and (Sprint 5 Commit 5C) calculated
/// performance intelligence. Owned by <see cref="SpecialistPageViewModel"/>,
/// constructed fresh whenever the selected specialist changes - same
/// per-selection child-ViewModel pattern <c>Customers.CustomerProfileViewModel</c>
/// established in Phase 10. No notes/timeline (unlike Customer 360) - not
/// requested for Specialists, keeping this foundation-scoped.
///
/// Sprint 5 Commit 5C: <see cref="PerformanceScore"/>/<see cref="PerformanceLevel"/>/
/// <see cref="RecommendationSignal"/>/<see cref="CompletedBookingCount"/>/
/// <see cref="CancelledBookingCount"/>/<see cref="NoShowBookingCount"/> mirror
/// <see cref="SpecialistIntelligenceDto"/> field-for-field - this ViewModel only
/// requests <see cref="IIntelligenceEngine.GetSpecialistIntelligenceAsync"/> and
/// picks out the entry matching <see cref="_specialistId"/>; every score,
/// level, and signal is still calculated entirely by Domain/Application (see
/// <c>Domain.Specialists.SpecialistPerformanceCalculator</c> and
/// <c>Application.Intelligence.IntelligenceEngine</c>), never here.
/// </summary>
public sealed partial class SpecialistProfileViewModel : ViewModelBase
{
    private readonly string _specialistId;
    private readonly ISpecialistProfileQueryService _profileQueryService;
    private readonly ISpecialistCommandService _commandService;
    private readonly IIntelligenceEngine _intelligenceEngine;
    private readonly IServiceQueryService _serviceQueryService;
    private readonly ISpecialistScheduleQueryService _scheduleQueryService;
    private readonly ISpecialistScheduleCommandService _scheduleCommandService;
    private readonly ILogger<SpecialistProfileViewModel> _logger;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private string? _saveErrorMessage;
    private bool _hasSaveError;
    private string? _assignmentErrorMessage;
    private bool _hasAssignmentError;
    private SpecialistDto? _specialist;
    private string _newSkillText = string.Empty;
    private SpecialistStatus _editableStatus;
    private ServiceDto? _selectedServiceToAssign;
    private bool _hasIntelligence;
    private int _performanceScore;
    private SpecialistPerformanceLevel _performanceLevel = SpecialistPerformanceLevel.Underperforming;
    private SpecialistRecommendationSignal _recommendationSignal = SpecialistRecommendationSignal.Attention;
    private int _completedBookingCount;
    private int _cancelledBookingCount;
    private int _noShowBookingCount;

    public SpecialistProfileViewModel(
        string specialistId,
        ISpecialistProfileQueryService profileQueryService,
        ISpecialistCommandService commandService,
        IIntelligenceEngine intelligenceEngine,
        IServiceQueryService serviceQueryService,
        ISpecialistScheduleQueryService scheduleQueryService,
        ISpecialistScheduleCommandService scheduleCommandService,
        ILogger<SpecialistScheduleViewModel>? scheduleLogger = null,
        ILogger<SpecialistAvailabilityViewModel>? availabilityLogger = null,
        ILogger<SpecialistProfileViewModel>? logger = null)
    {
        _specialistId = specialistId;
        _profileQueryService = profileQueryService;
        _commandService = commandService;
        _intelligenceEngine = intelligenceEngine;
        _serviceQueryService = serviceQueryService;
        _scheduleQueryService = scheduleQueryService;
        _scheduleCommandService = scheduleCommandService;
        _logger = logger ?? NullLogger<SpecialistProfileViewModel>.Instance;

        Skills = new ObservableCollection<SpecialistSkillDto>();
        AssignedServices = new ObservableCollection<AssignedServiceDto>();
        AvailableServicesToAssign = new ObservableCollection<ServiceDto>();

        // Phase 7.2.6 Shift Engine UI Activation: constructed once, alongside this ViewModel
        // itself, and never rebuilt on this ViewModel's own reloads - each self-loads
        // independently in its own constructor, same "per-selection child ViewModel" shape this
        // class already uses for itself (see this class's own doc comment).
        Schedule = new SpecialistScheduleViewModel(specialistId, scheduleQueryService, scheduleCommandService, scheduleLogger);
        Availability = new SpecialistAvailabilityViewModel(specialistId, scheduleQueryService, availabilityLogger);

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        AddSkillCommand = new AsyncRelayCommand(_ => AddSkillAsync(), _ => !string.IsNullOrWhiteSpace(NewSkillText));
        RemoveSkillCommand = new AsyncRelayCommand(parameter => RemoveSkillAsync(parameter as SpecialistSkillDto));
        SaveChangesCommand = new AsyncRelayCommand(_ => SaveChangesAsync());
        AssignServiceCommand = new AsyncRelayCommand(_ => AssignServiceAsync(), _ => SelectedServiceToAssign is not null);
        RemoveServiceAssignmentCommand = new AsyncRelayCommand(parameter => RemoveServiceAssignmentAsync(parameter as AssignedServiceDto));

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, same pattern as every
        // other page/profile ViewModel in this app.
        _ = LoadAsync();
    }

    /// <summary>
    /// Specialist Deactivation Wiring: raised after a successful save (see
    /// <see cref="SaveChangesAsync"/>) so <see cref="SpecialistPageViewModel"/> -
    /// the specialist directory this profile is shown alongside - can
    /// refresh its own list rather than keep showing this specialist's old
    /// status. This ViewModel has no reference back to its owning page
    /// (same one-way parent-constructs-child shape <c>Customers.CustomerProfileViewModel</c>
    /// already established), so a plain event is the wiring, not a new
    /// dependency in either direction.
    /// </summary>
    public event EventHandler? SpecialistUpdated;

    public ObservableCollection<SpecialistSkillDto> Skills { get; }

    /// <summary>Specialist-Service Assignment: the real, backend-confirmed services this specialist is eligible to perform - see <see cref="AssignedServiceDto"/>'s own doc comment.</summary>
    public ObservableCollection<AssignedServiceDto> AssignedServices { get; }

    /// <summary>Every catalog service not already in <see cref="AssignedServices"/> - the source list for the assign picker, recomputed on every <see cref="LoadAsync"/> so it never offers a service that is already assigned.</summary>
    public ObservableCollection<ServiceDto> AvailableServicesToAssign { get; }

    /// <summary>Phase 7.2.6 Shift Engine UI Activation - the Manager schedule UI (full read/write) for this specialist. See <see cref="SpecialistScheduleViewModel"/>'s own doc comment.</summary>
    public SpecialistScheduleViewModel Schedule { get; }

    /// <summary>Phase 7.2.6 Shift Engine UI Activation - the read-only availability view for this specialist. See <see cref="SpecialistAvailabilityViewModel"/>'s own doc comment.</summary>
    public SpecialistAvailabilityViewModel Availability { get; }

    public IReadOnlyList<SpecialistStatus> AvailableStatuses { get; } = Enum.GetValues<SpecialistStatus>();

    public ICommand LoadCommand { get; }

    public ICommand AddSkillCommand { get; }

    public ICommand RemoveSkillCommand { get; }

    public ICommand SaveChangesCommand { get; }

    public ICommand AssignServiceCommand { get; }

    public ICommand RemoveServiceAssignmentCommand { get; }

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

    /// <summary>
    /// Specialist Deactivation Wiring: a save-specific failure message,
    /// deliberately separate from <see cref="ErrorMessage"/>/<see cref="State"/> -
    /// those two drive <c>DashboardWidget</c>, which replaces this panel's
    /// entire content (status editor included) with a full error+retry
    /// view. A failed save must never hide the very form the user needs to
    /// see to retry, so it gets its own inline, non-destructive message
    /// instead. Never the raw <see cref="Exception.Message"/> - see
    /// <see cref="SaveChangesAsync"/>'s own doc comment for why.
    /// </summary>
    public string? SaveErrorMessage
    {
        get => _saveErrorMessage;
        private set => SetProperty(ref _saveErrorMessage, value);
    }

    /// <summary>Backs the inline error TextBlock's visibility - same explicit-companion-flag shape <see cref="HasIntelligence"/> already uses for <see cref="PerformanceScore"/>/etc. in this same class, rather than a computed property with no change notification of its own.</summary>
    public bool HasSaveError
    {
        get => _hasSaveError;
        private set => SetProperty(ref _hasSaveError, value);
    }

    /// <summary>Specialist-Service Assignment: same "separate, non-destructive, never-raw-exception-text" reasoning as <see cref="SaveErrorMessage"/>, kept deliberately distinct from it - a failed assignment and a failed status save are different operations and should not be reported through the same message.</summary>
    public string? AssignmentErrorMessage
    {
        get => _assignmentErrorMessage;
        private set => SetProperty(ref _assignmentErrorMessage, value);
    }

    public bool HasAssignmentError
    {
        get => _hasAssignmentError;
        private set => SetProperty(ref _hasAssignmentError, value);
    }

    public SpecialistDto? Specialist
    {
        get => _specialist;
        private set => SetProperty(ref _specialist, value);
    }

    public string NewSkillText
    {
        get => _newSkillText;
        set => SetProperty(ref _newSkillText, value);
    }

    /// <summary>Bound by the status ComboBox - a local edit buffer, synced from <see cref="Specialist"/> on every load so it always starts matching the persisted value.</summary>
    public SpecialistStatus EditableStatus
    {
        get => _editableStatus;
        set => SetProperty(ref _editableStatus, value);
    }

    /// <summary>Bound by the assign-service picker's ComboBox - a real <see cref="ServiceDto"/> from <see cref="AvailableServicesToAssign"/>, never free text (Specialist-Service Assignment's core data-model rule).</summary>
    public ServiceDto? SelectedServiceToAssign
    {
        get => _selectedServiceToAssign;
        set => SetProperty(ref _selectedServiceToAssign, value);
    }

    /// <summary>False until an <see cref="IIntelligenceEngine.GetSpecialistIntelligenceAsync"/> result matching <see cref="_specialistId"/> has loaded - lets the view distinguish "not loaded yet"/"no data" from a genuine zero score.</summary>
    public bool HasIntelligence
    {
        get => _hasIntelligence;
        private set => SetProperty(ref _hasIntelligence, value);
    }

    /// <summary>Sprint 5 Commit 5C - see this class's own doc comment.</summary>
    public int PerformanceScore
    {
        get => _performanceScore;
        private set => SetProperty(ref _performanceScore, value);
    }

    public SpecialistPerformanceLevel PerformanceLevel
    {
        get => _performanceLevel;
        private set => SetProperty(ref _performanceLevel, value);
    }

    public SpecialistRecommendationSignal RecommendationSignal
    {
        get => _recommendationSignal;
        private set => SetProperty(ref _recommendationSignal, value);
    }

    public int CompletedBookingCount
    {
        get => _completedBookingCount;
        private set => SetProperty(ref _completedBookingCount, value);
    }

    public int CancelledBookingCount
    {
        get => _cancelledBookingCount;
        private set => SetProperty(ref _cancelledBookingCount, value);
    }

    public int NoShowBookingCount
    {
        get => _noShowBookingCount;
        private set => SetProperty(ref _noShowBookingCount, value);
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;
        SaveErrorMessage = null;
        HasSaveError = false;
        AssignmentErrorMessage = null;
        HasAssignmentError = false;

        try
        {
            var profile = await _profileQueryService.GetProfileAsync(_specialistId).ConfigureAwait(true);

            Specialist = profile.Specialist;
            EditableStatus = profile.Specialist.Status;

            Skills.Clear();
            foreach (var skill in profile.Skills)
            {
                Skills.Add(skill);
            }

            AssignedServices.Clear();
            foreach (var assignment in profile.AssignedServices)
            {
                AssignedServices.Add(assignment);
            }

            await RefreshAvailableServicesToAssignAsync().ConfigureAwait(true);

            var intelligenceList = await _intelligenceEngine.GetSpecialistIntelligenceAsync().ConfigureAwait(true);
            var intelligence = intelligenceList.FirstOrDefault(entry => entry.SpecialistId == _specialistId);

            HasIntelligence = intelligence is not null;
            PerformanceScore = intelligence?.PerformanceScore ?? 0;
            PerformanceLevel = intelligence?.PerformanceLevel ?? SpecialistPerformanceLevel.Underperforming;
            RecommendationSignal = intelligence?.RecommendationSignal ?? SpecialistRecommendationSignal.Attention;
            CompletedBookingCount = intelligence?.CompletedBookingCount ?? 0;
            CancelledBookingCount = intelligence?.CancelledBookingCount ?? 0;
            NoShowBookingCount = intelligence?.NoShowBookingCount ?? 0;

            State = DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page/profile ViewModel in this app.
        catch (Exception)
#pragma warning restore CA1031
        {
            ErrorMessage = Strings.Common_ActionFailedMessage;
            State = DashboardState.Error;
            LogOperationFailed(nameof(LoadAsync));
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Specialist profile operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);

    private async Task AddSkillAsync()
    {
        try
        {
            await _commandService.AddSkillAsync(_specialistId, NewSkillText).ConfigureAwait(true);
            SaveErrorMessage = null;
            HasSaveError = false;
            NewSkillText = string.Empty;
            await LoadAsync().ConfigureAwait(true);
        }
#pragma warning disable CA1031 // Write boundary: any failure must surface as a safe inline message and preserve the form, never crash or leak internal detail - same justified broad catch as this class's own SaveChangesAsync boundary.
        catch (Exception)
#pragma warning restore CA1031
        {
            SaveErrorMessage = Strings.Specialists_SaveError;
            HasSaveError = true;
            LogOperationFailed(nameof(AddSkillAsync));
        }
    }

    private async Task RemoveSkillAsync(SpecialistSkillDto? skill)
    {
        if (skill is null)
        {
            return;
        }

        try
        {
            await _commandService.RemoveSkillAsync(_specialistId, skill.Id).ConfigureAwait(true);
            SaveErrorMessage = null;
            HasSaveError = false;
            await LoadAsync().ConfigureAwait(true);
        }
#pragma warning disable CA1031 // Write boundary - see AddSkillAsync's own justification.
        catch (Exception)
#pragma warning restore CA1031
        {
            SaveErrorMessage = Strings.Specialists_SaveError;
            HasSaveError = true;
            LogOperationFailed(nameof(RemoveSkillAsync));
        }
    }

    /// <summary>
    /// Specialist Deactivation Wiring. On success: clears any prior save
    /// error and refreshes the projection via <see cref="LoadAsync"/> (a
    /// live re-fetch from Backend - see <see cref="_commandService"/>'s own
    /// repository, there is no local specialist truth to update instead).
    /// On failure: never lets the exception escape (an unhandled fault
    /// here would otherwise reach <c>App.xaml.cs</c>'s global dialog with a
    /// raw, internal, developer-facing message - not what an owner should
    /// see), and never leaves <see cref="EditableStatus"/> showing a status
    /// change Backend never actually accepted - it reverts to
    /// <see cref="Specialist"/>'s last known-good value, the same
    /// "never treat a rejected local edit as if it were applied" rule the
    /// architecture rules require of this whole vertical.
    /// </summary>
    private async Task SaveChangesAsync()
    {
        if (Specialist is null)
        {
            return;
        }

        var request = new UpdateSpecialistRequest(
            Specialist.Id,
            Specialist.FullName,
            Specialist.Title,
            Specialist.Email,
            Specialist.Phone,
            EditableStatus,
            Specialist.Bio);

        try
        {
            await _commandService.UpdateSpecialistAsync(request).ConfigureAwait(true);
            SaveErrorMessage = null;
            HasSaveError = false;
            await LoadAsync().ConfigureAwait(true);
            SpecialistUpdated?.Invoke(this, EventArgs.Empty);
        }
#pragma warning disable CA1031 // Save boundary: any failure must surface as a safe, user-facing message and leave the ViewModel reflecting the specialist's real last-known-good state, never crash or leak internal exception detail - same justified broad catch as LoadAsync's own top-level boundary in this class.
        catch (Exception)
#pragma warning restore CA1031
        {
            EditableStatus = Specialist.Status;
            SaveErrorMessage = Strings.Specialists_SaveError;
            HasSaveError = true;
            LogOperationFailed(nameof(SaveChangesAsync));
        }
    }

    /// <summary>Recomputes <see cref="AvailableServicesToAssign"/> as the full catalog minus <see cref="AssignedServices"/> - called after every load so the picker never re-offers an already-assigned service.</summary>
    private async Task RefreshAvailableServicesToAssignAsync()
    {
        var catalog = await _serviceQueryService.GetServicesAsync().ConfigureAwait(true);
        var assignedServiceIds = AssignedServices.Select(assignment => assignment.ServiceId).ToHashSet();

        AvailableServicesToAssign.Clear();
        foreach (var service in catalog.Where(service => !assignedServiceIds.Contains(service.Id)))
        {
            AvailableServicesToAssign.Add(service);
        }
    }

    /// <summary>
    /// Specialist-Service Assignment. Real identifiers only -
    /// <see cref="SelectedServiceToAssign"/> is a real <see cref="ServiceDto"/>
    /// from the catalog, never free text. Same success/failure shape as
    /// <see cref="SaveChangesAsync"/>: on success, refreshes the projection
    /// (<see cref="LoadAsync"/>, a live Backend re-fetch - no local
    /// assignment truth is ever recorded here); on failure, never lets the
    /// exception escape and never mutates <see cref="AssignedServices"/>/
    /// <see cref="AvailableServicesToAssign"/> at all (both are only ever
    /// touched inside <see cref="LoadAsync"/>, which only runs after a
    /// confirmed success) - so a rejected assignment leaves the UI exactly
    /// as it was, never a corrupted or half-applied state.
    /// </summary>
    private async Task AssignServiceAsync()
    {
        if (SelectedServiceToAssign is null)
        {
            return;
        }

        var serviceId = SelectedServiceToAssign.Id;

        try
        {
            await _commandService.AssignServiceAsync(_specialistId, serviceId).ConfigureAwait(true);
            AssignmentErrorMessage = null;
            HasAssignmentError = false;
            SelectedServiceToAssign = null;
            await LoadAsync().ConfigureAwait(true);
        }
#pragma warning disable CA1031 // Same justified broad catch as SaveChangesAsync's own boundary in this class - see its doc comment.
        catch (Exception)
#pragma warning restore CA1031
        {
            AssignmentErrorMessage = Strings.Specialists_AssignmentError;
            HasAssignmentError = true;
            LogOperationFailed(nameof(AssignServiceAsync));
        }
    }

    private async Task RemoveServiceAssignmentAsync(AssignedServiceDto? assignment)
    {
        if (assignment is null)
        {
            return;
        }

        try
        {
            await _commandService.RemoveServiceAssignmentAsync(_specialistId, assignment.ServiceId).ConfigureAwait(true);
            AssignmentErrorMessage = null;
            HasAssignmentError = false;
            await LoadAsync().ConfigureAwait(true);
        }
#pragma warning disable CA1031 // Same justified broad catch as SaveChangesAsync's own boundary in this class - see its doc comment.
        catch (Exception)
#pragma warning restore CA1031
        {
            AssignmentErrorMessage = Strings.Specialists_AssignmentError;
            HasAssignmentError = true;
            LogOperationFailed(nameof(RemoveServiceAssignmentAsync));
        }
    }
}
