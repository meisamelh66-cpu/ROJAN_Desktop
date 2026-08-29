using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.Inventory;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.Inventory;

/// <summary>
/// Drives the product profile panel for one selected product - details,
/// current stock level, a "record a stock transaction" mini form,
/// transaction history, and linked services (service-to-product
/// mappings) plus a "map to service" mini form. Owned by
/// <see cref="InventoryPageViewModel"/>, constructed fresh whenever the
/// selected product changes - same per-selection child-ViewModel pattern
/// <c>Customers.CustomerProfileViewModel</c> established in Phase 10.
/// </summary>
public sealed partial class InventoryProfileViewModel : ViewModelBase
{
    private readonly string _productId;
    private readonly IProductProfileQueryService _profileQueryService;
    private readonly IInventoryCommandService _commandService;
    private readonly ILogger<InventoryProfileViewModel> _logger;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private string? _actionErrorMessage;
    private bool _hasActionError;
    private ProductDto? _product;
    private InventoryItemDto? _stock;
    private StockTransactionType _selectedTransactionType = StockTransactionType.Received;
    private int _transactionQuantity = 1;
    private string _transactionNotes = string.Empty;
    private string _newMappingServiceName = string.Empty;
    private int _newMappingQuantityPerService = 1;

    public InventoryProfileViewModel(
        string productId,
        IProductProfileQueryService profileQueryService,
        IInventoryCommandService commandService,
        ILogger<InventoryProfileViewModel>? logger = null)
    {
        _productId = productId;
        _profileQueryService = profileQueryService;
        _commandService = commandService;
        _logger = logger ?? NullLogger<InventoryProfileViewModel>.Instance;

        RecentTransactions = new ObservableCollection<StockTransactionDto>();
        ServiceMappings = new ObservableCollection<ServiceProductMappingDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        RecordTransactionCommand = new AsyncRelayCommand(_ => RecordTransactionAsync(), _ => TransactionQuantity != 0);
        MapServiceCommand = new AsyncRelayCommand(_ => MapServiceAsync(), _ => !string.IsNullOrWhiteSpace(NewMappingServiceName) && NewMappingQuantityPerService > 0);
        UnmapServiceCommand = new AsyncRelayCommand(parameter => UnmapServiceAsync(parameter as ServiceProductMappingDto));

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, same pattern as every
        // other page/profile ViewModel in this app.
        _ = LoadAsync();
    }

    public ObservableCollection<StockTransactionDto> RecentTransactions { get; }

    public ObservableCollection<ServiceProductMappingDto> ServiceMappings { get; }

    public IReadOnlyList<StockTransactionType> AvailableTransactionTypes { get; } =
        Enum.GetValues<StockTransactionType>();

    public ICommand LoadCommand { get; }

    public ICommand RecordTransactionCommand { get; }

    public ICommand MapServiceCommand { get; }

    public ICommand UnmapServiceCommand { get; }

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
    /// Non-destructive inline error shown when a record-transaction / map /
    /// unmap command fails - unlike <see cref="ErrorMessage"/>/<see cref="State"/>
    /// it never blanks the panel. Production Hardening missing-guard sweep
    /// (Wave C), same shape as HR.EmployeeProfileViewModel.ActionErrorMessage.
    /// </summary>
    public string? ActionErrorMessage
    {
        get => _actionErrorMessage;
        private set => SetProperty(ref _actionErrorMessage, value);
    }

    public bool HasActionError
    {
        get => _hasActionError;
        private set => SetProperty(ref _hasActionError, value);
    }

    public ProductDto? Product
    {
        get => _product;
        private set => SetProperty(ref _product, value);
    }

    public InventoryItemDto? Stock
    {
        get => _stock;
        private set => SetProperty(ref _stock, value);
    }

    public StockTransactionType SelectedTransactionType
    {
        get => _selectedTransactionType;
        set => SetProperty(ref _selectedTransactionType, value);
    }

    public int TransactionQuantity
    {
        get => _transactionQuantity;
        set => SetProperty(ref _transactionQuantity, value);
    }

    public string TransactionNotes
    {
        get => _transactionNotes;
        set => SetProperty(ref _transactionNotes, value);
    }

    public string NewMappingServiceName
    {
        get => _newMappingServiceName;
        set => SetProperty(ref _newMappingServiceName, value);
    }

    public int NewMappingQuantityPerService
    {
        get => _newMappingQuantityPerService;
        set => SetProperty(ref _newMappingQuantityPerService, value);
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var profile = await _profileQueryService.GetProfileAsync(_productId).ConfigureAwait(true);

            Product = profile.Product;
            Stock = profile.Stock;

            RecentTransactions.Clear();
            foreach (var transaction in profile.RecentTransactions)
            {
                RecentTransactions.Add(transaction);
            }

            ServiceMappings.Clear();
            foreach (var mapping in profile.ServiceMappings)
            {
                ServiceMappings.Add(mapping);
            }

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

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Inventory profile operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);

    private async Task RecordTransactionAsync()
    {
        try
        {
            await _commandService.RecordStockTransactionAsync(_productId, SelectedTransactionType, TransactionQuantity, TransactionNotes).ConfigureAwait(true);
            ActionErrorMessage = null;
            HasActionError = false;

            TransactionQuantity = 1;
            TransactionNotes = string.Empty;
            await LoadAsync().ConfigureAwait(true);
        }
#pragma warning disable CA1031 // Command boundary: a failed write must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(RecordTransactionAsync));
        }
    }

    private async Task MapServiceAsync()
    {
        try
        {
            await _commandService.MapProductToServiceAsync(_productId, NewMappingServiceName, NewMappingQuantityPerService).ConfigureAwait(true);
            ActionErrorMessage = null;
            HasActionError = false;

            NewMappingServiceName = string.Empty;
            NewMappingQuantityPerService = 1;
            await LoadAsync().ConfigureAwait(true);
        }
#pragma warning disable CA1031 // Command boundary: a failed write must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(MapServiceAsync));
        }
    }

    private async Task UnmapServiceAsync(ServiceProductMappingDto? mapping)
    {
        if (mapping is null)
        {
            return;
        }

        try
        {
            await _commandService.UnmapProductFromServiceAsync(_productId, mapping.Id).ConfigureAwait(true);
            ActionErrorMessage = null;
            HasActionError = false;

            await LoadAsync().ConfigureAwait(true);
        }
#pragma warning disable CA1031 // Command boundary: a failed write must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(UnmapServiceAsync));
        }
    }
}
