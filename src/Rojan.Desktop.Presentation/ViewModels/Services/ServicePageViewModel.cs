using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Intelligence;
using Rojan.Desktop.Application.Services;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Services;

/// <summary>
/// Drives ServicePage - the service catalog/search on the left, and the
/// selected service's <see cref="ServiceProfileViewModel"/> (category,
/// duration, price, description, assigned specialists, and Sprint 5
/// Commit 5C's popularity intelligence) on the right. Depends only on
/// Application services (<see cref="IServiceQueryService"/>,
/// <see cref="IServiceProfileQueryService"/>, <see cref="IServiceCommandService"/>,
/// <see cref="IIntelligenceEngine"/>), consistent with Presentation never
/// reaching past Application into Domain/Infrastructure. Reuses
/// <see cref="DashboardState"/> rather than a duplicate enum, same
/// reasoning as every other page ViewModel in this app. No "New Service"
/// form (unlike Customers/Bookings/Specialists) - catalog authoring
/// wasn't requested for this phase, only browse/search plus specialist
/// assignment.
///
/// Sprint 5 Commit 2 (Premium Service Search &amp; Filters): <see cref="SearchText"/>/
/// <see cref="SelectedCategory"/>/<see cref="SelectedStatus"/>/
/// <see cref="MinDuration"/>/<see cref="MaxDuration"/>/<see cref="MinPrice"/>/
/// <see cref="MaxPrice"/> are combined into one <see cref="ServiceSearchFilter"/>
/// and run through <see cref="IServiceQueryService.SearchServicesAsync(ServiceSearchFilter, CancellationToken)"/> -
/// every load (including the initial one) goes through this same method
/// now, not a separate <c>GetServicesAsync</c>/<c>SearchServicesAsync(string)</c>
/// path, the same unification <c>Customers.CustomerPageViewModel</c>/
/// <c>Bookings.BookingPageViewModel</c> already established in Sprint 3/4.
/// An all-default filter is equivalent to the old unfiltered
/// <c>GetServicesAsync</c> call - see <see cref="ServiceSearchFilter"/>'s
/// own doc comment. Same <c>_filterVersion</c> staleness-guard pattern as
/// every other page ViewModel with combinable filters.
/// </summary>
public sealed partial class ServicePageViewModel : ViewModelBase
{
    private readonly IServiceQueryService _queryService;
    private readonly IServiceProfileQueryService _profileQueryService;
    private readonly IServiceCommandService _commandService;
    private readonly IIntelligenceEngine _intelligenceEngine;
    private readonly ILogger<ServicePageViewModel> _logger;
    private readonly ILoggerFactory? _loggerFactory;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private bool _isRefreshing;
    private bool _isReplacingServices;
    private string _searchText = string.Empty;
    private ServiceCategory? _selectedCategory;
    private ServiceStatus? _selectedStatus;
    private int? _minDuration;
    private int? _maxDuration;
    private decimal? _minPrice;
    private decimal? _maxPrice;
    private int _resultCount;
    private ServiceDto? _selectedService;
    private ServiceProfileViewModel? _profile;
    private string _newServiceName = string.Empty;
    private string _newServiceDescription = string.Empty;
    private int _newServiceDurationMinutes;
    private decimal _newServicePrice;
    private ServiceCategoryOptionDto? _selectedCategoryForNewService;
    private string? _createErrorMessage;
    private bool _hasCreateError;

    /// <summary>Incremented on every filter/load-triggering change - see <c>Bookings.BookingPageViewModel</c>'s field of the same name for the full reasoning.</summary>
    private int _filterVersion;

    public ServicePageViewModel(
        IServiceQueryService queryService,
        IServiceProfileQueryService profileQueryService,
        IServiceCommandService commandService,
        IIntelligenceEngine intelligenceEngine,
        ILogger<ServicePageViewModel>? logger = null,
        ILoggerFactory? loggerFactory = null)
    {
        _queryService = queryService;
        _profileQueryService = profileQueryService;
        _commandService = commandService;
        _intelligenceEngine = intelligenceEngine;
        _logger = logger ?? NullLogger<ServicePageViewModel>.Instance;
        _loggerFactory = loggerFactory;

        Services = new ObservableCollection<ServiceDto>();
        AvailableCategories = new ObservableCollection<ServiceCategoryOptionDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        SearchCommand = new AsyncRelayCommand(_ => LoadAsync());
        ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
        CreateServiceCommand = new AsyncRelayCommand(_ => CreateServiceAsync(), _ => CanCreateService());

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, so there is nothing
        // left that could become an unobserved task exception.
        _ = LoadAsync();

        // Service Catalog Authoring: categories are unrelated to the filtered service list, so they're
        // loaded once here, not re-fetched on every filter change LoadAsync responds to. A failure here
        // is swallowed deliberately - see LoadCategoriesAsync's own doc comment.
        _ = LoadCategoriesAsync();
    }

    public ObservableCollection<ServiceDto> Services { get; }

    /// <summary>Service Catalog Authoring: the real, selectable categories the Create Service picker binds to - never free text.</summary>
    public ObservableCollection<ServiceCategoryOptionDto> AvailableCategories { get; }

    /// <summary>Re-runs the load - bound as the Retry action on DashboardWidget's Error state.</summary>
    public ICommand LoadCommand { get; }

    /// <summary>Explicit re-run of the current filter combination - the reactive filter-property setters already trigger a load on every change, so this exists for an explicit Search action (button/Enter key) a future UI pass may wire up.</summary>
    public ICommand SearchCommand { get; }

    /// <summary>Resets every filter property to its default (empty/null) and reloads - equivalent to a freshly-opened, unfiltered catalog view.</summary>
    public ICommand ClearFiltersCommand { get; }

    /// <summary>Service Catalog Authoring.</summary>
    public ICommand CreateServiceCommand { get; }

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

    /// <summary>Page Stability: true while an already-displayed list is reloaded in place (the list stays visible) - see <see cref="DashboardStateExtensions"/>.</summary>
    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set => SetProperty(ref _isRefreshing, value);
    }

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

    /// <summary>Null means "every category" - the first entry of <see cref="AvailableCategoryOptions"/>.</summary>
    public ServiceCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                _ = LoadAsync();
            }
        }
    }

    /// <summary>Null means "every status" - the first entry of <see cref="AvailableStatusOptions"/>.</summary>
    public ServiceStatus? SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public int? MinDuration
    {
        get => _minDuration;
        set
        {
            if (SetProperty(ref _minDuration, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public int? MaxDuration
    {
        get => _maxDuration;
        set
        {
            if (SetProperty(ref _maxDuration, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public decimal? MinPrice
    {
        get => _minPrice;
        set
        {
            if (SetProperty(ref _minPrice, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public decimal? MaxPrice
    {
        get => _maxPrice;
        set
        {
            if (SetProperty(ref _maxPrice, value))
            {
                _ = LoadAsync();
            }
        }
    }

    /// <summary>How many services the current filter combination matched - <see cref="Services"/>.Count kept as its own bindable property so a "N results" caption doesn't need to bind the collection itself.</summary>
    public int ResultCount
    {
        get => _resultCount;
        private set => SetProperty(ref _resultCount, value);
    }

    /// <summary>Bindable options for the category filter ComboBox - leads with <c>null</c> ("every category") followed by every real <see cref="ServiceCategory"/> value.</summary>
    public IReadOnlyList<ServiceCategory?> AvailableCategoryOptions { get; } =
        new ServiceCategory?[] { null }.Concat(Enum.GetValues<ServiceCategory>().Cast<ServiceCategory?>()).ToList();

    /// <summary>Bindable options for the status filter ComboBox - leads with <c>null</c> ("every status") followed by every real <see cref="ServiceStatus"/> value.</summary>
    public IReadOnlyList<ServiceStatus?> AvailableStatusOptions { get; } =
        new ServiceStatus?[] { null }.Concat(Enum.GetValues<ServiceStatus>().Cast<ServiceStatus?>()).ToList();

    public ServiceDto? SelectedService
    {
        get => _selectedService;
        set
        {
            // Page Stability: Services.Clear() inside ReplaceAll makes the bound ListBox push null back
            // here - that is the collection being rebuilt, not the user deselecting, so it must not
            // tear down the detail panel ReplaceAll is about to keep by Id.
            if (value is null && _isReplacingServices)
            {
                return;
            }

            var previousId = _selectedService?.Id;

            // A refreshed row is a new record instance for the same entity - only a genuinely
            // different selection rebuilds the detail panel (and loses its in-progress edits).
            if (SetProperty(ref _selectedService, value) && value?.Id != previousId)
            {
                Profile = value is null
                    ? null
                    : new ServiceProfileViewModel(value.Id, _profileQueryService, _commandService, _intelligenceEngine, _loggerFactory?.CreateLogger<ServiceProfileViewModel>());
            }
        }
    }

    /// <summary>Profile for <see cref="SelectedService"/> - null when nothing is selected.</summary>
    public ServiceProfileViewModel? Profile
    {
        get => _profile;
        private set => SetProperty(ref _profile, value);
    }

    // Service Catalog Authoring: "New Service" fields - same shape as Specialists.SpecialistPageViewModel's
    // own "New Specialist" fields, the direct structural precedent for this section.

    public string NewServiceName
    {
        get => _newServiceName;
        set => SetProperty(ref _newServiceName, value);
    }

    public string NewServiceDescription
    {
        get => _newServiceDescription;
        set => SetProperty(ref _newServiceDescription, value);
    }

    public int NewServiceDurationMinutes
    {
        get => _newServiceDurationMinutes;
        set => SetProperty(ref _newServiceDurationMinutes, value);
    }

    public decimal NewServicePrice
    {
        get => _newServicePrice;
        set => SetProperty(ref _newServicePrice, value);
    }

    /// <summary>Bound by the Create Service picker's ComboBox - a real <see cref="ServiceCategoryOptionDto"/> from <see cref="AvailableCategories"/>, never free text (Service Catalog Authoring's core data-model rule).</summary>
    public ServiceCategoryOptionDto? SelectedCategoryForNewService
    {
        get => _selectedCategoryForNewService;
        set => SetProperty(ref _selectedCategoryForNewService, value);
    }

    /// <summary>Non-destructive, never-raw-exception-text inline error for a failed Create - same reasoning as Specialists.SpecialistProfileViewModel's own AssignmentErrorMessage.</summary>
    public string? CreateErrorMessage
    {
        get => _createErrorMessage;
        private set => SetProperty(ref _createErrorMessage, value);
    }

    public bool HasCreateError
    {
        get => _hasCreateError;
        private set => SetProperty(ref _hasCreateError, value);
    }

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
            var services = await _queryService.SearchServicesAsync(BuildFilter()).ConfigureAwait(true);

            if (requestVersion != _filterVersion)
            {
                // A newer filter change (or another reload) started after
                // this one - its result will win instead, so applying this
                // now-stale response would flash outdated data.
                return;
            }

            ReplaceAll(services);

            State = services.Count == 0
                ? DashboardState.Empty
                : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - this is the one place a broad catch is the correct behavior, not a code smell.
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

    // Security: logs the operation name only - never the exception, its message,
    // service/catalog data, or any backend response detail.
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Service page operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);

    /// <summary>
    /// Service Catalog Authoring. Failure is deliberately swallowed (no
    /// ErrorMessage/State change) rather than surfaced as a page-level
    /// error - a category-load failure only degrades the Create form's own
    /// picker to empty, it must never hide the entire, otherwise-healthy
    /// catalog list behind an error view.
    /// </summary>
    private async Task LoadCategoriesAsync()
    {
#pragma warning disable CA1031 // Deliberately swallowed - see this method's own doc comment.
        try
        {
            var categories = await _queryService.GetCategoriesAsync().ConfigureAwait(true);
            AvailableCategories.Clear();
            foreach (var category in categories)
            {
                AvailableCategories.Add(category);
            }
        }
        catch (Exception)
        {
            // Swallowed by design - see this method's own doc comment. A log
            // entry is the only trail for this otherwise-silent degradation.
            LogOperationFailed(nameof(LoadCategoriesAsync));
        }
#pragma warning restore CA1031
    }

    private bool CanCreateService() =>
        !string.IsNullOrWhiteSpace(NewServiceName) && NewServiceDurationMinutes > 0 && NewServicePrice > 0m && SelectedCategoryForNewService is not null;

    /// <summary>
    /// Service Catalog Authoring. Real identifiers only -
    /// <see cref="SelectedCategoryForNewService"/> is a real
    /// <see cref="ServiceCategoryOptionDto"/> from <see cref="AvailableCategories"/>,
    /// never free text. On success: clears the form and reloads the
    /// catalog (a live Backend re-fetch - no local service truth is ever
    /// recorded here), then selects the newly-created service. On failure:
    /// never lets the exception escape, never mutates <see cref="Services"/>,
    /// and preserves the form's contents so the user can retry without
    /// re-typing.
    /// </summary>
    private async Task CreateServiceAsync()
    {
        if (SelectedCategoryForNewService is null)
        {
            return;
        }

        var request = new CreateServiceRequest(
            SelectedCategoryForNewService.Id, NewServiceName, NewServiceDescription, NewServiceDurationMinutes, NewServicePrice);

        try
        {
            var created = await _commandService.CreateServiceAsync(request).ConfigureAwait(true);
            CreateErrorMessage = null;
            HasCreateError = false;
            NewServiceName = string.Empty;
            NewServiceDescription = string.Empty;
            NewServiceDurationMinutes = 0;
            NewServicePrice = 0m;
            SelectedCategoryForNewService = null;
            await LoadAsync().ConfigureAwait(true);
            SelectedService = Services.FirstOrDefault(service => service.Id == created.Id);
        }
#pragma warning disable CA1031 // Save boundary: any failure must surface as a safe, user-facing message and preserve the form's contents, never crash or leak internal exception detail - same justified broad catch as every other write in this app.
        catch (Exception)
#pragma warning restore CA1031
        {
            CreateErrorMessage = Strings.Services_SaveError;
            HasCreateError = true;
            LogOperationFailed(nameof(CreateServiceAsync));
        }
    }

    private ServiceSearchFilter BuildFilter() => new(
        SearchText: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
        Category: SelectedCategory,
        Status: SelectedStatus,
        MinDurationMinutes: MinDuration,
        MaxDurationMinutes: MaxDuration,
        MinPrice: MinPrice,
        MaxPrice: MaxPrice);

    private void ClearFilters()
    {
        _searchText = string.Empty;
        _selectedCategory = null;
        _selectedStatus = null;
        _minDuration = null;
        _maxDuration = null;
        _minPrice = null;
        _maxPrice = null;

        // Raise every property-changed notification once, then reload once - setting each property
        // individually would fire LoadAsync repeatedly (once per property) for a single user action.
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(SelectedCategory));
        OnPropertyChanged(nameof(SelectedStatus));
        OnPropertyChanged(nameof(MinDuration));
        OnPropertyChanged(nameof(MaxDuration));
        OnPropertyChanged(nameof(MinPrice));
        OnPropertyChanged(nameof(MaxPrice));

        _ = LoadAsync();
    }

    /// <summary>
    /// Page Stability: rebuilds <see cref="Services"/> while keeping the current selection by its stable Id
    /// (a reload returns new record instances, so the old reference-and-value check lost the
    /// selection after every edit and jumped to the first row, rebuilding the detail panel). Falls
    /// back to the first row only when there was no selection, or the selected item is no longer in
    /// the (filtered) result.
    /// </summary>
    private void ReplaceAll(IReadOnlyList<ServiceDto> services)
    {
        var selectedId = SelectedService?.Id;

        _isReplacingServices = true;
        try
        {
            Services.Clear();
            foreach (var item in services)
            {
                Services.Add(item);
            }
        }
        finally
        {
            _isReplacingServices = false;
        }

        ResultCount = Services.Count;

        var preserved = selectedId is null ? null : Services.FirstOrDefault(item => item.Id == selectedId);
        SelectedService = preserved ?? (Services.Count > 0 ? Services[0] : null);

        // Re-announce even when the preserved record compares equal to the old one: Clear() already
        // reset the bound ListBox's own SelectedItem, which only re-syncs on a change notification.
        OnPropertyChanged(nameof(SelectedService));
    }
}
