using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Customers;

/// <summary>
/// Drives CustomerPage - the customer list/search on the left, and (Phase
/// 10) the Customer 360 <see cref="CustomerProfileViewModel"/> for
/// whichever customer is selected on the right. Depends only on
/// Application services (<see cref="ICustomerQueryService"/>,
/// <see cref="ICustomerProfileQueryService"/>, <see cref="ICustomerCommandService"/>),
/// consistent with Presentation never reaching past Application into
/// Domain/Infrastructure. Reuses <see cref="DashboardState"/> rather than a
/// duplicate enum, same reasoning as every other page ViewModel.
/// </summary>
public sealed class CustomerPageViewModel : ViewModelBase
{
    private readonly ICustomerQueryService _queryService;
    private readonly ICustomerProfileQueryService _profileQueryService;
    private readonly ICustomerCommandService _commandService;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private string _searchText = string.Empty;
    private CustomerDto? _selectedCustomer;
    private CustomerProfileViewModel? _profile;
    private string _newCustomerFullName = string.Empty;
    private string _newCustomerCompany = string.Empty;
    private string _newCustomerEmail = string.Empty;
    private string _newCustomerPhone = string.Empty;

    public CustomerPageViewModel(
        ICustomerQueryService queryService,
        ICustomerProfileQueryService profileQueryService,
        ICustomerCommandService commandService)
    {
        _queryService = queryService;
        _profileQueryService = profileQueryService;
        _commandService = commandService;

        Customers = new ObservableCollection<CustomerDto>();
        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        CreateCustomerCommand = new AsyncRelayCommand(
            _ => CreateCustomerAsync(),
            _ => !string.IsNullOrWhiteSpace(NewCustomerFullName));

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, so there is nothing
        // left that could become an unobserved task exception.
        _ = LoadAsync();
    }

    public ObservableCollection<CustomerDto> Customers { get; }

    /// <summary>Re-runs the load - bound as the Retry action on DashboardWidget's Error state.</summary>
    public ICommand LoadCommand { get; }

    public ICommand CreateCustomerCommand { get; }

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

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (SetProperty(ref _selectedCustomer, value))
            {
                Profile = value is null
                    ? null
                    : new CustomerProfileViewModel(value.Id, _profileQueryService, _commandService);
            }
        }
    }

    /// <summary>Customer 360 profile for <see cref="SelectedCustomer"/> - null when nothing is selected.</summary>
    public CustomerProfileViewModel? Profile
    {
        get => _profile;
        private set => SetProperty(ref _profile, value);
    }

    public string NewCustomerFullName
    {
        get => _newCustomerFullName;
        set => SetProperty(ref _newCustomerFullName, value);
    }

    public string NewCustomerCompany
    {
        get => _newCustomerCompany;
        set => SetProperty(ref _newCustomerCompany, value);
    }

    public string NewCustomerEmail
    {
        get => _newCustomerEmail;
        set => SetProperty(ref _newCustomerEmail, value);
    }

    public string NewCustomerPhone
    {
        get => _newCustomerPhone;
        set => SetProperty(ref _newCustomerPhone, value);
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var customers = await _queryService.GetCustomersAsync().ConfigureAwait(true);
            ReplaceAll(customers);

            State = customers.Count == 0
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
    /// Runs the search through <see cref="ICustomerQueryService.SearchCustomersAsync"/>
    /// rather than filtering a client-side cache - Search is now a real
    /// Application use case (Phase 10), not View-layer logic. Guards
    /// against out-of-order completions: if the user kept typing after
    /// this call started, <paramref name="searchText"/> no longer matches
    /// <see cref="SearchText"/> by the time the result arrives, and the
    /// stale result is discarded.
    /// </summary>
    private async Task SearchAsync(string searchText)
    {
        try
        {
            var results = await _queryService.SearchCustomersAsync(searchText).ConfigureAwait(true);
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

    private async Task CreateCustomerAsync()
    {
        var request = new CreateCustomerRequest(NewCustomerFullName, NewCustomerCompany, NewCustomerEmail, NewCustomerPhone, string.Empty);
        var created = await _commandService.CreateCustomerAsync(request).ConfigureAwait(true);

        NewCustomerFullName = string.Empty;
        NewCustomerCompany = string.Empty;
        NewCustomerEmail = string.Empty;
        NewCustomerPhone = string.Empty;

        await LoadAsync().ConfigureAwait(true);
        SelectedCustomer = Customers.FirstOrDefault(customer => customer.Id == created.Id);
    }

    private void ReplaceAll(IReadOnlyList<CustomerDto> customers)
    {
        Customers.Clear();
        foreach (var customer in customers)
        {
            Customers.Add(customer);
        }

        if (SelectedCustomer is null || !Customers.Contains(SelectedCustomer))
        {
            SelectedCustomer = Customers.Count > 0 ? Customers[0] : null;
        }
    }
}
