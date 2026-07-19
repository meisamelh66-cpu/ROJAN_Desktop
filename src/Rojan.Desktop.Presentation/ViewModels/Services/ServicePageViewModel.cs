using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Services;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Services;

/// <summary>
/// Drives ServicePage - the service catalog/search on the left, and the
/// selected service's <see cref="ServiceProfileViewModel"/> (category,
/// duration, price, description, assigned specialists) on the right.
/// Depends only on Application services (<see cref="IServiceQueryService"/>,
/// <see cref="IServiceProfileQueryService"/>, <see cref="IServiceCommandService"/>),
/// consistent with Presentation never reaching past Application into
/// Domain/Infrastructure. Reuses <see cref="DashboardState"/> rather than
/// a duplicate enum, same reasoning as every other page ViewModel in this
/// app. No "New Service" form (unlike Customers/Bookings/Specialists) -
/// catalog authoring wasn't requested for this phase, only browse/search
/// plus specialist assignment.
/// </summary>
public sealed class ServicePageViewModel : ViewModelBase
{
    private readonly IServiceQueryService _queryService;
    private readonly IServiceProfileQueryService _profileQueryService;
    private readonly IServiceCommandService _commandService;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private string _searchText = string.Empty;
    private ServiceDto? _selectedService;
    private ServiceProfileViewModel? _profile;

    public ServicePageViewModel(
        IServiceQueryService queryService,
        IServiceProfileQueryService profileQueryService,
        IServiceCommandService commandService)
    {
        _queryService = queryService;
        _profileQueryService = profileQueryService;
        _commandService = commandService;

        Services = new ObservableCollection<ServiceDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, so there is nothing
        // left that could become an unobserved task exception.
        _ = LoadAsync();
    }

    public ObservableCollection<ServiceDto> Services { get; }

    /// <summary>Re-runs the load - bound as the Retry action on DashboardWidget's Error state.</summary>
    public ICommand LoadCommand { get; }

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
                _ = SearchAsync(value);
            }
        }
    }

    public ServiceDto? SelectedService
    {
        get => _selectedService;
        set
        {
            if (SetProperty(ref _selectedService, value))
            {
                Profile = value is null
                    ? null
                    : new ServiceProfileViewModel(value.Id, _profileQueryService, _commandService);
            }
        }
    }

    /// <summary>Profile for <see cref="SelectedService"/> - null when nothing is selected.</summary>
    public ServiceProfileViewModel? Profile
    {
        get => _profile;
        private set => SetProperty(ref _profile, value);
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var services = await _queryService.GetServicesAsync().ConfigureAwait(true);
            ReplaceAll(services);

            State = services.Count == 0
                ? DashboardState.Empty
                : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - this is the one place a broad catch is the correct behavior, not a code smell.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
        }
    }

    /// <summary>
    /// Runs the search through <see cref="IServiceQueryService.SearchServicesAsync"/>
    /// rather than filtering a client-side cache - same reasoning as
    /// <c>Customers.CustomerPageViewModel.SearchAsync</c>. Guards against
    /// out-of-order completions: if the user kept typing after this call
    /// started, <paramref name="searchText"/> no longer matches
    /// <see cref="SearchText"/> by the time the result arrives, and the
    /// stale result is discarded.
    /// </summary>
    private async Task SearchAsync(string searchText)
    {
        try
        {
            var results = await _queryService.SearchServicesAsync(searchText).ConfigureAwait(true);
            if (!string.Equals(searchText, SearchText, StringComparison.Ordinal))
            {
                return;
            }

            ReplaceAll(results);
        }
#pragma warning disable CA1031 // Same top-level boundary reasoning as LoadAsync - a failed search must surface as the Error state, not crash the page.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            if (string.Equals(searchText, SearchText, StringComparison.Ordinal))
            {
                ErrorMessage = exception.Message;
                State = DashboardState.Error;
            }
        }
    }

    private void ReplaceAll(IReadOnlyList<ServiceDto> services)
    {
        Services.Clear();
        foreach (var service in services)
        {
            Services.Add(service);
        }

        if (SelectedService is null || !Services.Contains(SelectedService))
        {
            SelectedService = Services.Count > 0 ? Services[0] : null;
        }
    }
}
