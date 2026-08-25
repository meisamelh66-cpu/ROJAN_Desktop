using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Intelligence;
using Rojan.Desktop.Application.Services;
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
public sealed class ServicePageViewModel : ViewModelBase
{
    private readonly IServiceQueryService _queryService;
    private readonly IServiceProfileQueryService _profileQueryService;
    private readonly IServiceCommandService _commandService;
    private readonly IIntelligenceEngine _intelligenceEngine;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
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
    private ServiceCategoryDto? _newServiceCategory;

    /// <summary>Incremented on every filter/load-triggering change - see <c>Bookings.BookingPageViewModel</c>'s field of the same name for the full reasoning.</summary>
    private int _filterVersion;

    public ServicePageViewModel(
        IServiceQueryService queryService,
        IServiceProfileQueryService profileQueryService,
        IServiceCommandService commandService,
        IIntelligenceEngine intelligenceEngine)
    {
        _queryService = queryService;
        _profileQueryService = profileQueryService;
        _commandService = commandService;
        _intelligenceEngine = intelligenceEngine;

        Services = new ObservableCollection<ServiceDto>();
        AvailableCategories = new ObservableCollection<ServiceCategoryDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        SearchCommand = new AsyncRelayCommand(_ => LoadAsync());
        ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
        CreateServiceCommand = new AsyncRelayCommand(
            _ => CreateServiceAsync(),
            _ => !string.IsNullOrWhiteSpace(NewServiceName) && NewServiceCategory is not null);

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, so there is nothing
        // left that could become an unobserved task exception. Same
        // reasoning for the one-time category load below - a failed load
        // just leaves the picker empty (CreateServiceCommand's CanExecute
        // already requires a selection), not a crash.
        _ = LoadAsync();
        _ = LoadCategoriesAsync();
    }

    public ObservableCollection<ServiceDto> Services { get; }

    /// <summary>Real salon categories for the New Service picker - loaded once, not re-run on every filter change (unlike <see cref="Services"/>).</summary>
    public ObservableCollection<ServiceCategoryDto> AvailableCategories { get; }

    /// <summary>Re-runs the load - bound as the Retry action on DashboardWidget's Error state.</summary>
    public ICommand LoadCommand { get; }

    /// <summary>Explicit re-run of the current filter combination - the reactive filter-property setters already trigger a load on every change, so this exists for an explicit Search action (button/Enter key) a future UI pass may wire up.</summary>
    public ICommand SearchCommand { get; }

    /// <summary>Resets every filter property to its default (empty/null) and reloads - equivalent to a freshly-opened, unfiltered catalog view.</summary>
    public ICommand ClearFiltersCommand { get; }

    /// <summary>Service Catalog Management: the one Presentation entry point for <see cref="IServiceCommandService.CreateServiceAsync"/> - mirrors <c>Specialists.SpecialistPageViewModel.CreateSpecialistCommand</c>'s shape.</summary>
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
            if (SetProperty(ref _selectedService, value))
            {
                Profile = value is null
                    ? null
                    : new ServiceProfileViewModel(value.Id, _profileQueryService, _commandService, _intelligenceEngine);
            }
        }
    }

    /// <summary>Profile for <see cref="SelectedService"/> - null when nothing is selected.</summary>
    public ServiceProfileViewModel? Profile
    {
        get => _profile;
        private set => SetProperty(ref _profile, value);
    }

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

    /// <summary>Bound by the New Service category ComboBox - a real salon category from <see cref="AvailableCategories"/>, never the fixed <see cref="ServiceCategory"/> classification enum, since Backend routes a create by real category id.</summary>
    public ServiceCategoryDto? NewServiceCategory
    {
        get => _newServiceCategory;
        set => SetProperty(ref _newServiceCategory, value);
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _queryService.GetCategoriesAsync().ConfigureAwait(true);

        AvailableCategories.Clear();
        foreach (var category in categories)
        {
            AvailableCategories.Add(category);
        }
    }

    private async Task CreateServiceAsync()
    {
        if (NewServiceCategory is null)
        {
            return;
        }

        var request = new CreateServiceRequest(NewServiceName, NewServiceCategory.Id, NewServiceDurationMinutes, NewServicePrice, NewServiceDescription);
        var created = await _commandService.CreateServiceAsync(request).ConfigureAwait(true);

        NewServiceName = string.Empty;
        NewServiceDescription = string.Empty;
        NewServiceDurationMinutes = 0;
        NewServicePrice = 0m;
        NewServiceCategory = null;

        await LoadAsync().ConfigureAwait(true);
        SelectedService = Services.FirstOrDefault(service => service.Id == created.Id);
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

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
        catch (Exception exception)
#pragma warning restore CA1031
        {
            if (requestVersion == _filterVersion)
            {
                ErrorMessage = exception.Message;
                State = DashboardState.Error;
            }
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

    private void ReplaceAll(IReadOnlyList<ServiceDto> services)
    {
        Services.Clear();
        foreach (var service in services)
        {
            Services.Add(service);
        }

        ResultCount = Services.Count;

        if (SelectedService is null || !Services.Contains(SelectedService))
        {
            SelectedService = Services.Count > 0 ? Services[0] : null;
        }
    }
}
