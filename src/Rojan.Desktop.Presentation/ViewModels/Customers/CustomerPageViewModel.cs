using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Customers;

/// <summary>
/// Drives CustomerPage. Loads real (fake-repository-backed) data through
/// <see cref="ICustomerQueryService"/> - the only Application dependency
/// this ViewModel has, consistent with Presentation never reaching past
/// Application into Domain/Infrastructure. Reuses <see cref="DashboardState"/>
/// rather than a duplicate enum: it already models exactly the four states
/// a repository-backed load can be in, and reusing it lets this page reuse
/// <c>DashboardWidget</c> (Controls/Dashboard) unchanged.
/// </summary>
public sealed class CustomerPageViewModel : ViewModelBase
{
    private readonly ICustomerQueryService _queryService;
    private IReadOnlyList<CustomerDto> _allCustomers = [];
    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private string _searchText = string.Empty;
    private CustomerDto? _selectedCustomer;

    public CustomerPageViewModel(ICustomerQueryService queryService)
    {
        _queryService = queryService;

        Customers = new ObservableCollection<CustomerDto>();
        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, so there is nothing
        // left that could become an unobserved task exception.
        _ = LoadAsync();
    }

    public ObservableCollection<CustomerDto> Customers { get; }

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
                ApplyFilter();
            }
        }
    }

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetProperty(ref _selectedCustomer, value);
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            _allCustomers = await _queryService.GetCustomersAsync().ConfigureAwait(true);
            ApplyFilter();

            State = _allCustomers.Count == 0
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

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allCustomers
            : _allCustomers
                .Where(customer =>
                    customer.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    customer.Company.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    customer.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

        Customers.Clear();
        foreach (var customer in filtered)
        {
            Customers.Add(customer);
        }

        if (SelectedCustomer is null || !Customers.Contains(SelectedCustomer))
        {
            SelectedCustomer = Customers.Count > 0 ? Customers[0] : null;
        }
    }
}
