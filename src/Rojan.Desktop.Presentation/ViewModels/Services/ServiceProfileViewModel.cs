using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Intelligence;
using Rojan.Desktop.Application.Services;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Services;

/// <summary>
/// Drives the service profile panel for one selected service - category,
/// duration, price, and description display, assigned specialists, and
/// (Sprint 5 Commit 5C) calculated popularity intelligence. Owned by
/// <see cref="ServicePageViewModel"/>, constructed fresh whenever the
/// selected service changes - same per-selection child-ViewModel pattern
/// <c>Customers.CustomerProfileViewModel</c> established in Phase 10. No
/// editable status/category (unlike Customer/Specialist profiles) -
/// Phase 13 only requested display for those fields, plus the one real
/// write capability: specialist assignment.
///
/// Sprint 5 Commit 5C: <see cref="PopularityScore"/>/<see cref="PopularityLevel"/>/
/// <see cref="RecommendationSignal"/>/<see cref="CompletedBookingCount"/>/
/// <see cref="UpcomingBookingCount"/> mirror <see cref="ServiceIntelligenceDto"/>
/// field-for-field - this ViewModel only requests
/// <see cref="IIntelligenceEngine.GetServiceIntelligenceAsync"/> and picks out
/// the entry matching <see cref="_serviceId"/>; every score, level, and
/// signal is still calculated entirely by Domain/Application (see
/// <c>Domain.Services.ServicePopularityCalculator</c> and
/// <c>Application.Intelligence.IntelligenceEngine</c>), never here.
/// </summary>
public sealed class ServiceProfileViewModel : ViewModelBase
{
    private readonly string _serviceId;
    private readonly IServiceProfileQueryService _profileQueryService;
    private readonly IServiceCommandService _commandService;
    private readonly IIntelligenceEngine _intelligenceEngine;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private ServiceDto? _service;
    private string _newSpecialistName = string.Empty;
    private string _editableName = string.Empty;
    private string _editableDescription = string.Empty;
    private int _editableDurationMinutes;
    private decimal _editablePrice;
    private string? _saveErrorMessage;
    private bool _hasSaveError;
    private bool _hasIntelligence;
    private int _popularityScore;
    private ServicePopularityLevel _popularityLevel = ServicePopularityLevel.LowDemand;
    private ServiceRecommendationSignal _recommendationSignal = ServiceRecommendationSignal.Reconsider;
    private int _completedBookingCount;
    private int _upcomingBookingCount;

    public ServiceProfileViewModel(
        string serviceId,
        IServiceProfileQueryService profileQueryService,
        IServiceCommandService commandService,
        IIntelligenceEngine intelligenceEngine)
    {
        _serviceId = serviceId;
        _profileQueryService = profileQueryService;
        _commandService = commandService;
        _intelligenceEngine = intelligenceEngine;

        AssignedSpecialists = new ObservableCollection<AssignedSpecialistDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        AssignSpecialistCommand = new AsyncRelayCommand(_ => AssignSpecialistAsync(), _ => !string.IsNullOrWhiteSpace(NewSpecialistName));
        UnassignSpecialistCommand = new AsyncRelayCommand(parameter => UnassignSpecialistAsync(parameter as AssignedSpecialistDto));
        SaveChangesCommand = new AsyncRelayCommand(_ => SaveChangesAsync());
        DeactivateCommand = new AsyncRelayCommand(_ => DeactivateAsync(), _ => Service?.Status == ServiceStatus.Active);

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, same pattern as every
        // other page/profile ViewModel in this app.
        _ = LoadAsync();
    }

    public ObservableCollection<AssignedSpecialistDto> AssignedSpecialists { get; }

    public ICommand LoadCommand { get; }

    public ICommand AssignSpecialistCommand { get; }

    public ICommand UnassignSpecialistCommand { get; }

    /// <summary>Service Catalog Authoring.</summary>
    public ICommand SaveChangesCommand { get; }

    /// <summary>
    /// Service Catalog Authoring. Only supports Active -&gt; Discontinued,
    /// matching <see cref="Domain.Services.IServiceRepository.UpdateServiceAsync"/>'s
    /// own scope - deliberately no reactivation, no Seasonal, no general
    /// status picker. Enabled only when the currently-loaded service is
    /// Active - a plain UI-level check, not a call into Domain
    /// (<c>ServicePageViewModel</c>'s own doc comment already establishes
    /// Presentation never reaches past Application into Domain).
    /// </summary>
    public ICommand DeactivateCommand { get; }

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

    public ServiceDto? Service
    {
        get => _service;
        private set => SetProperty(ref _service, value);
    }

    public string NewSpecialistName
    {
        get => _newSpecialistName;
        set => SetProperty(ref _newSpecialistName, value);
    }

    /// <summary>Service Catalog Authoring: local edit buffer synced from <see cref="Service"/> on every load - not committed until <see cref="SaveChangesCommand"/> runs, same "buffer separate from source-of-truth" pattern as <c>Specialists.SpecialistProfileViewModel.EditableStatus</c>.</summary>
    public string EditableName
    {
        get => _editableName;
        set => SetProperty(ref _editableName, value);
    }

    public string EditableDescription
    {
        get => _editableDescription;
        set => SetProperty(ref _editableDescription, value);
    }

    public int EditableDurationMinutes
    {
        get => _editableDurationMinutes;
        set => SetProperty(ref _editableDurationMinutes, value);
    }

    public decimal EditablePrice
    {
        get => _editablePrice;
        set => SetProperty(ref _editablePrice, value);
    }

    /// <summary>Service Catalog Authoring: shared by <see cref="SaveChangesAsync"/> and <see cref="DeactivateAsync"/> - both are "manage this catalog entry" concerns on the same panel, unlike Specialist's two-separate-message-pairs design.</summary>
    public string? SaveErrorMessage
    {
        get => _saveErrorMessage;
        private set => SetProperty(ref _saveErrorMessage, value);
    }

    public bool HasSaveError
    {
        get => _hasSaveError;
        private set => SetProperty(ref _hasSaveError, value);
    }

    /// <summary>False until an <see cref="IIntelligenceEngine.GetServiceIntelligenceAsync"/> result matching <see cref="_serviceId"/> has loaded - lets the view distinguish "not loaded yet"/"no data" from a genuine zero score.</summary>
    public bool HasIntelligence
    {
        get => _hasIntelligence;
        private set => SetProperty(ref _hasIntelligence, value);
    }

    /// <summary>Sprint 5 Commit 5C - see this class's own doc comment.</summary>
    public int PopularityScore
    {
        get => _popularityScore;
        private set => SetProperty(ref _popularityScore, value);
    }

    public ServicePopularityLevel PopularityLevel
    {
        get => _popularityLevel;
        private set => SetProperty(ref _popularityLevel, value);
    }

    public ServiceRecommendationSignal RecommendationSignal
    {
        get => _recommendationSignal;
        private set => SetProperty(ref _recommendationSignal, value);
    }

    public int CompletedBookingCount
    {
        get => _completedBookingCount;
        private set => SetProperty(ref _completedBookingCount, value);
    }

    public int UpcomingBookingCount
    {
        get => _upcomingBookingCount;
        private set => SetProperty(ref _upcomingBookingCount, value);
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var profile = await _profileQueryService.GetProfileAsync(_serviceId).ConfigureAwait(true);

            Service = profile.Service;
            EditableName = profile.Service.Name;
            EditableDescription = profile.Service.Description;
            EditableDurationMinutes = profile.Service.DurationMinutes;
            EditablePrice = profile.Service.PriceValue;

            AssignedSpecialists.Clear();
            foreach (var assignment in profile.AssignedSpecialists)
            {
                AssignedSpecialists.Add(assignment);
            }

            var intelligenceList = await _intelligenceEngine.GetServiceIntelligenceAsync().ConfigureAwait(true);
            var intelligence = intelligenceList.FirstOrDefault(entry => entry.ServiceId == _serviceId);

            HasIntelligence = intelligence is not null;
            PopularityScore = intelligence?.PopularityScore ?? 0;
            PopularityLevel = intelligence?.PopularityLevel ?? ServicePopularityLevel.LowDemand;
            RecommendationSignal = intelligence?.RecommendationSignal ?? ServiceRecommendationSignal.Reconsider;
            CompletedBookingCount = intelligence?.CompletedBookingCount ?? 0;
            UpcomingBookingCount = intelligence?.UpcomingBookingCount ?? 0;

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

    private async Task AssignSpecialistAsync()
    {
        await _commandService.AssignSpecialistAsync(_serviceId, NewSpecialistName).ConfigureAwait(true);
        NewSpecialistName = string.Empty;
        await LoadAsync().ConfigureAwait(true);
    }

    private async Task UnassignSpecialistAsync(AssignedSpecialistDto? assignment)
    {
        if (assignment is null)
        {
            return;
        }

        await _commandService.UnassignSpecialistAsync(_serviceId, assignment.Id).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Service Catalog Authoring. Commits the edit buffers only - never
    /// touches <see cref="Domain.Services.Service.Status"/>, so a Save can
    /// never accidentally deactivate/reactivate a service.
    /// <see cref="Domain.Services.Service.CategoryId"/> is carried through
    /// unchanged from the currently-loaded <see cref="Service"/>, since
    /// Backend's update contract has no field to change it (see that
    /// field's own doc comment) - this is never sourced from any editable
    /// UI control.
    /// </summary>
    private async Task SaveChangesAsync()
    {
        if (Service is null)
        {
            return;
        }

        var request = new UpdateServiceRequest(
            Service.Id, Service.CategoryId, EditableName, EditableDescription,
            EditableDurationMinutes, EditablePrice, Service.Status);

        try
        {
            await _commandService.UpdateServiceAsync(request).ConfigureAwait(true);
            SaveErrorMessage = null;
            HasSaveError = false;
            await LoadAsync().ConfigureAwait(true);
        }
#pragma warning disable CA1031 // Safe-failure boundary: never let a save error crash the panel or discard the user's edit buffer - revert to last-known-good instead, same reasoning as ServicePageViewModel.CreateServiceAsync.
        catch (Exception)
#pragma warning restore CA1031
        {
            SaveErrorMessage = Strings.Services_SaveError;
            HasSaveError = true;

            // Revert the edit buffers to the last-known-good Service state,
            // so a failed Save never leaves stale, unsaved text on screen
            // pretending to be committed.
            if (Service is not null)
            {
                EditableName = Service.Name;
                EditableDescription = Service.Description;
                EditableDurationMinutes = Service.DurationMinutes;
                EditablePrice = Service.PriceValue;
            }
        }
    }

    /// <summary>
    /// Service Catalog Authoring. Only supported transition:
    /// Active -&gt; Discontinued. Uses <see cref="Service"/>'s own
    /// currently-loaded field values (never the unsaved
    /// <see cref="EditableName"/>-style buffers) so a Deactivate can never
    /// smuggle in an unsaved edit - Save and Deactivate are two
    /// independent actions.
    /// </summary>
    private async Task DeactivateAsync()
    {
        if (Service is null)
        {
            return;
        }

        var request = new UpdateServiceRequest(
            Service.Id, Service.CategoryId, Service.Name, Service.Description,
            Service.DurationMinutes, Service.PriceValue, ServiceStatus.Discontinued);

        try
        {
            await _commandService.UpdateServiceAsync(request).ConfigureAwait(true);
            SaveErrorMessage = null;
            HasSaveError = false;
            await LoadAsync().ConfigureAwait(true);
        }
#pragma warning disable CA1031 // Safe-failure boundary - see SaveChangesAsync's own justification.
        catch (Exception)
#pragma warning restore CA1031
        {
            SaveErrorMessage = Strings.Services_SaveError;
            HasSaveError = true;
        }
    }
}
