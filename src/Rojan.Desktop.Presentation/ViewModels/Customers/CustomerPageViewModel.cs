using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Presentation.Localization;
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
/// <see cref="SearchText"/>/<see cref="StatusFilter"/>/<see cref="CompanyFilter"/>/
/// <see cref="TagFilter"/> (Sprint 4 Commit 2) are combined into one
/// <see cref="CustomerSearchFilter"/> and run through
/// <see cref="ICustomerQueryService.SearchCustomersAsync(CustomerSearchFilter, CancellationToken)"/> -
/// every load (including the initial one and every post-create reload) goes
/// through this same method now, not a separate <c>GetCustomersAsync</c>
/// path, so an active filter survives a Create action instead of silently
/// resetting. An all-default filter is equivalent to the old unfiltered
/// <c>GetCustomersAsync</c> call - see <see cref="CustomerSearchFilter"/>'s
/// own doc comment. Same <c>_filterVersion</c> staleness-guard pattern as
/// <c>Bookings.BookingPageViewModel</c>.
/// </summary>
public sealed partial class CustomerPageViewModel : ViewModelBase
{
    private readonly ICustomerQueryService _queryService;
    private readonly ICustomerProfileQueryService _profileQueryService;
    private readonly ICustomerCommandService _commandService;
    private readonly ILogger<CustomerPageViewModel> _logger;
    private readonly ILoggerFactory? _loggerFactory;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private bool _isRefreshing;
    private bool _isReplacingCustomers;
    private string? _createErrorMessage;
    private bool _hasCreateError;
    private string _searchText = string.Empty;
    private string _companyFilter = string.Empty;
    private string _tagFilter = string.Empty;
    private CustomerStatus? _statusFilter;
    private CustomerDto? _selectedCustomer;
    private CustomerProfileViewModel? _profile;
    private string _newCustomerFullName = string.Empty;
    private string _newCustomerCompany = string.Empty;
    private string _newCustomerEmail = string.Empty;
    private string _newCustomerPhone = string.Empty;

    /// <summary>Incremented on every filter/load-triggering change - see <c>Bookings.BookingPageViewModel</c>'s field of the same name for the full reasoning.</summary>
    private int _filterVersion;

    public CustomerPageViewModel(
        ICustomerQueryService queryService,
        ICustomerProfileQueryService profileQueryService,
        ICustomerCommandService commandService,
        ILogger<CustomerPageViewModel>? logger = null,
        ILoggerFactory? loggerFactory = null)
    {
        _queryService = queryService;
        _profileQueryService = profileQueryService;
        _commandService = commandService;
        _logger = logger ?? NullLogger<CustomerPageViewModel>.Instance;
        _loggerFactory = loggerFactory;

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

    /// <summary>Page Stability: true while an already-displayed list is reloaded in place (the list stays visible) - see <see cref="DashboardStateExtensions"/>.</summary>
    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set => SetProperty(ref _isRefreshing, value);
    }

    /// <summary>
    /// Production Hardening (missing-guard sweep, Wave A): a create-specific
    /// failure message for a failed new-customer submission, deliberately
    /// separate from <see cref="ErrorMessage"/>/<see cref="State"/> - those two
    /// replace the whole page with an error+retry view, which would discard
    /// the quick-add form the user needs to retry. Same reasoning and shape as
    /// <c>Services.ServicePageViewModel.CreateErrorMessage</c>. Never the raw
    /// <see cref="System.Exception.Message"/>.
    /// </summary>
    public string? CreateErrorMessage
    {
        get => _createErrorMessage;
        private set => SetProperty(ref _createErrorMessage, value);
    }

    /// <summary>Backs the inline error TextBlock's visibility - same explicit-companion-flag shape as <c>Services.ServicePageViewModel.HasCreateError</c>.</summary>
    public bool HasCreateError
    {
        get => _hasCreateError;
        private set => SetProperty(ref _hasCreateError, value);
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

    public string CompanyFilter
    {
        get => _companyFilter;
        set
        {
            if (SetProperty(ref _companyFilter, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public string TagFilter
    {
        get => _tagFilter;
        set
        {
            if (SetProperty(ref _tagFilter, value))
            {
                _ = LoadAsync();
            }
        }
    }

    /// <summary>Null means "every status" - the first entry of <see cref="AvailableStatusOptions"/>.</summary>
    public CustomerStatus? StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                _ = LoadAsync();
            }
        }
    }

    /// <summary>Bindable options for the status filter ComboBox - leads with <c>null</c> ("every status") followed by every real <see cref="CustomerStatus"/> value.</summary>
    public IReadOnlyList<CustomerStatus?> AvailableStatusOptions { get; } =
        new CustomerStatus?[] { null }.Concat(Enum.GetValues<CustomerStatus>().Cast<CustomerStatus?>()).ToList();

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            // Page Stability: Customers.Clear() inside ReplaceAll makes the bound ListBox push null back
            // here - that is the collection being rebuilt, not the user deselecting, so it must not
            // tear down the detail panel ReplaceAll is about to keep by Id.
            if (value is null && _isReplacingCustomers)
            {
                return;
            }

            var previousId = _selectedCustomer?.Id;

            // A refreshed row is a new record instance for the same entity - only a genuinely
            // different selection rebuilds the detail panel (and loses its in-progress edits).
            if (SetProperty(ref _selectedCustomer, value) && value?.Id != previousId)
            {
                Profile = value is null
                    ? null
                    : new CustomerProfileViewModel(value.Id, _profileQueryService, _commandService, _loggerFactory?.CreateLogger<CustomerProfileViewModel>());
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
            var customers = await _queryService.SearchCustomersAsync(BuildFilter()).ConfigureAwait(true);

            if (requestVersion != _filterVersion)
            {
                // A newer filter change (or another reload) started after
                // this one - its result will win instead, so applying this
                // now-stale response would flash outdated data.
                return;
            }

            ReplaceAll(customers);

            State = customers.Count == 0
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
    // customer data, or any backend response detail.
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Customer page operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);

    private CustomerSearchFilter BuildFilter() => new(
        SearchText: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
        Company: string.IsNullOrWhiteSpace(CompanyFilter) ? null : CompanyFilter,
        Status: StatusFilter,
        Tag: string.IsNullOrWhiteSpace(TagFilter) ? null : TagFilter);

    private async Task CreateCustomerAsync()
    {
        var request = new CreateCustomerRequest(NewCustomerFullName, NewCustomerCompany, NewCustomerEmail, NewCustomerPhone, string.Empty);

        try
        {
            var created = await _commandService.CreateCustomerAsync(request).ConfigureAwait(true);
            CreateErrorMessage = null;
            HasCreateError = false;

            NewCustomerFullName = string.Empty;
            NewCustomerCompany = string.Empty;
            NewCustomerEmail = string.Empty;
            NewCustomerPhone = string.Empty;

            await LoadAsync().ConfigureAwait(true);
            SelectedCustomer = Customers.FirstOrDefault(customer => customer.Id == created.Id);
        }
#pragma warning disable CA1031 // Create boundary: any failure must surface as a safe inline message and preserve the form's contents, never crash or leak internal detail - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync.
        catch (Exception)
#pragma warning restore CA1031
        {
            CreateErrorMessage = Strings.Common_ActionFailedMessage;
            HasCreateError = true;
            LogOperationFailed(nameof(CreateCustomerAsync));
        }
    }

    /// <summary>
    /// Page Stability: rebuilds <see cref="Customers"/> while keeping the current selection by its stable Id
    /// (a reload returns new record instances, so the old reference-and-value check lost the
    /// selection after every edit and jumped to the first row, rebuilding the detail panel). Falls
    /// back to the first row only when there was no selection, or the selected item is no longer in
    /// the (filtered) result.
    /// </summary>
    private void ReplaceAll(IReadOnlyList<CustomerDto> customers)
    {
        var selectedId = SelectedCustomer?.Id;

        _isReplacingCustomers = true;
        try
        {
            Customers.Clear();
            foreach (var item in customers)
            {
                Customers.Add(item);
            }
        }
        finally
        {
            _isReplacingCustomers = false;
        }

        var preserved = selectedId is null ? null : Customers.FirstOrDefault(item => item.Id == selectedId);
        SelectedCustomer = preserved ?? (Customers.Count > 0 ? Customers[0] : null);

        // Re-announce even when the preserved record compares equal to the old one: Clear() already
        // reset the bound ListBox's own SelectedItem, which only re-syncs on a change notification.
        OnPropertyChanged(nameof(SelectedCustomer));
    }
}
