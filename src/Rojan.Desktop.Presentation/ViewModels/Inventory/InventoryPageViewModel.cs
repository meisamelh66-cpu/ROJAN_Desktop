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
/// Drives InventoryPage - the product catalog/search plus low-stock count
/// on the left (with a quick-add form for new products, categories, and
/// suppliers), and the selected product's <see cref="InventoryProfileViewModel"/>
/// (stock level, transactions, service mappings) on the right. Depends
/// only on Application services (<see cref="IProductQueryService"/>,
/// <see cref="IProductProfileQueryService"/>, <see cref="IInventoryQueryService"/>,
/// <see cref="IInventoryCommandService"/>), consistent with Presentation
/// never reaching past Application into Domain/Infrastructure. Reuses
/// <see cref="DashboardState"/> rather than a duplicate enum, same
/// reasoning as every other page ViewModel in this app.
/// </summary>
public sealed partial class InventoryPageViewModel : ViewModelBase
{
    private readonly IProductQueryService _queryService;
    private readonly IProductProfileQueryService _profileQueryService;
    private readonly IInventoryQueryService _inventoryQueryService;
    private readonly IInventoryCommandService _commandService;
    private readonly ILogger<InventoryPageViewModel> _logger;
    private readonly ILoggerFactory? _loggerFactory;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private string? _actionErrorMessage;
    private bool _hasActionError;
    private string _searchText = string.Empty;
    private ProductDto? _selectedProduct;
    private InventoryProfileViewModel? _profile;
    private int _lowStockCount;

    private string _newProductSku = string.Empty;
    private string _newProductName = string.Empty;
    private string _newProductDescription = string.Empty;
    private string _newProductUnitPrice = string.Empty;
    private ProductCategoryDto? _selectedNewProductCategory;
    private SupplierDto? _selectedNewProductSupplier;
    private int _newProductInitialQuantity = 1;
    private int _newProductReorderThreshold = 5;

    private string _newCategoryName = string.Empty;
    private string _newSupplierName = string.Empty;

    public InventoryPageViewModel(
        IProductQueryService queryService,
        IProductProfileQueryService profileQueryService,
        IInventoryQueryService inventoryQueryService,
        IInventoryCommandService commandService,
        ILogger<InventoryPageViewModel>? logger = null,
        ILoggerFactory? loggerFactory = null)
    {
        _queryService = queryService;
        _profileQueryService = profileQueryService;
        _inventoryQueryService = inventoryQueryService;
        _commandService = commandService;
        _logger = logger ?? NullLogger<InventoryPageViewModel>.Instance;
        _loggerFactory = loggerFactory;

        Products = new ObservableCollection<ProductDto>();
        Categories = new ObservableCollection<ProductCategoryDto>();
        Suppliers = new ObservableCollection<SupplierDto>();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        CreateProductCommand = new AsyncRelayCommand(
            _ => CreateProductAsync(),
            _ => !string.IsNullOrWhiteSpace(NewProductSku)
                && !string.IsNullOrWhiteSpace(NewProductName)
                && SelectedNewProductCategory is not null
                && SelectedNewProductSupplier is not null);
        AddCategoryCommand = new AsyncRelayCommand(_ => AddCategoryAsync(), _ => !string.IsNullOrWhiteSpace(NewCategoryName));
        AddSupplierCommand = new AsyncRelayCommand(_ => AddSupplierAsync(), _ => !string.IsNullOrWhiteSpace(NewSupplierName));

        // Safe fire-and-forget: LoadAsync catches every failure internally
        // and represents it via State/ErrorMessage, so there is nothing
        // left that could become an unobserved task exception.
        _ = LoadAsync();
    }

    public ObservableCollection<ProductDto> Products { get; }

    public ObservableCollection<ProductCategoryDto> Categories { get; }

    public ObservableCollection<SupplierDto> Suppliers { get; }

    /// <summary>Re-runs the load - bound as the Retry action on DashboardWidget's Error state.</summary>
    public ICommand LoadCommand { get; }

    public ICommand CreateProductCommand { get; }

    public ICommand AddCategoryCommand { get; }

    public ICommand AddSupplierCommand { get; }

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
    /// Non-destructive inline error shown when a user-triggered write command
    /// (create product, add category/supplier) fails - unlike
    /// <see cref="ErrorMessage"/>/<see cref="State"/> it never blanks the page.
    /// Production Hardening missing-guard sweep (Wave C), same shape as
    /// HR.HrPageViewModel.ActionErrorMessage.
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

    /// <summary>Count of products at or below their reorder threshold - the low-stock monitoring signal shown at the top of the page.</summary>
    public int LowStockCount
    {
        get => _lowStockCount;
        private set => SetProperty(ref _lowStockCount, value);
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

    public ProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                Profile = value is null
                    ? null
                    : new InventoryProfileViewModel(value.Id, _profileQueryService, _commandService, _loggerFactory?.CreateLogger<InventoryProfileViewModel>());
            }
        }
    }

    /// <summary>Profile for <see cref="SelectedProduct"/> - null when nothing is selected.</summary>
    public InventoryProfileViewModel? Profile
    {
        get => _profile;
        private set => SetProperty(ref _profile, value);
    }

    public string NewProductSku
    {
        get => _newProductSku;
        set => SetProperty(ref _newProductSku, value);
    }

    public string NewProductName
    {
        get => _newProductName;
        set => SetProperty(ref _newProductName, value);
    }

    public string NewProductDescription
    {
        get => _newProductDescription;
        set => SetProperty(ref _newProductDescription, value);
    }

    public string NewProductUnitPrice
    {
        get => _newProductUnitPrice;
        set => SetProperty(ref _newProductUnitPrice, value);
    }

    public ProductCategoryDto? SelectedNewProductCategory
    {
        get => _selectedNewProductCategory;
        set => SetProperty(ref _selectedNewProductCategory, value);
    }

    public SupplierDto? SelectedNewProductSupplier
    {
        get => _selectedNewProductSupplier;
        set => SetProperty(ref _selectedNewProductSupplier, value);
    }

    public int NewProductInitialQuantity
    {
        get => _newProductInitialQuantity;
        set => SetProperty(ref _newProductInitialQuantity, value);
    }

    public int NewProductReorderThreshold
    {
        get => _newProductReorderThreshold;
        set => SetProperty(ref _newProductReorderThreshold, value);
    }

    public string NewCategoryName
    {
        get => _newCategoryName;
        set => SetProperty(ref _newCategoryName, value);
    }

    public string NewSupplierName
    {
        get => _newSupplierName;
        set => SetProperty(ref _newSupplierName, value);
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            var products = await _queryService.GetProductsAsync().ConfigureAwait(true);
            ReplaceProducts(products);

            var categories = await _queryService.GetCategoriesAsync().ConfigureAwait(true);
            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            var suppliers = await _queryService.GetSuppliersAsync().ConfigureAwait(true);
            Suppliers.Clear();
            foreach (var supplier in suppliers)
            {
                Suppliers.Add(supplier);
            }

            var lowStockItems = await _inventoryQueryService.GetLowStockItemsAsync().ConfigureAwait(true);
            LowStockCount = lowStockItems.Count;

            State = products.Count == 0
                ? DashboardState.Empty
                : DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page ViewModel in this app.
        catch (Exception)
#pragma warning restore CA1031
        {
            ErrorMessage = Strings.Common_ActionFailedMessage;
            State = DashboardState.Error;
            LogOperationFailed(nameof(LoadAsync));
        }
    }

    // Security: logs the operation name only - never the exception, its message,
    // product/supplier data, or any backend response detail.
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Inventory page operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);

    /// <summary>
    /// Runs the search through <see cref="IProductQueryService.SearchProductsAsync"/>
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
            var results = await _queryService.SearchProductsAsync(searchText).ConfigureAwait(true);
            if (!string.Equals(searchText, SearchText, StringComparison.Ordinal))
            {
                return;
            }

            ReplaceProducts(results);
        }
#pragma warning disable CA1031 // Same top-level boundary reasoning as LoadAsync - a failed search must surface as the Error state, not crash the page.
        catch (Exception)
#pragma warning restore CA1031
        {
            if (string.Equals(searchText, SearchText, StringComparison.Ordinal))
            {
                ErrorMessage = Strings.Common_ActionFailedMessage;
                State = DashboardState.Error;
                LogOperationFailed(nameof(SearchAsync));
            }
        }
    }

    private void ReplaceProducts(IReadOnlyList<ProductDto> products)
    {
        Products.Clear();
        foreach (var product in products)
        {
            Products.Add(product);
        }

        if (SelectedProduct is null || !Products.Contains(SelectedProduct))
        {
            SelectedProduct = Products.Count > 0 ? Products[0] : null;
        }
    }

    private async Task CreateProductAsync()
    {
        if (SelectedNewProductCategory is null || SelectedNewProductSupplier is null)
        {
            return;
        }

        var request = new CreateProductRequest(
            NewProductSku,
            NewProductName,
            SelectedNewProductCategory.Id,
            SelectedNewProductCategory.Name,
            SelectedNewProductSupplier.Id,
            SelectedNewProductSupplier.Name,
            string.IsNullOrWhiteSpace(NewProductUnitPrice) ? "0 تومان" : NewProductUnitPrice,
            NewProductDescription,
            NewProductInitialQuantity,
            NewProductReorderThreshold);

        try
        {
            var created = await _commandService.CreateProductAsync(request).ConfigureAwait(true);
            ActionErrorMessage = null;
            HasActionError = false;

            NewProductSku = string.Empty;
            NewProductName = string.Empty;
            NewProductDescription = string.Empty;
            NewProductUnitPrice = string.Empty;
            NewProductInitialQuantity = 1;
            NewProductReorderThreshold = 5;

            await LoadAsync().ConfigureAwait(true);
            SelectedProduct = Products.FirstOrDefault(product => product.Id == created.Id);
        }
#pragma warning disable CA1031 // Command boundary: a failed write must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(CreateProductAsync));
        }
    }

    private async Task AddCategoryAsync()
    {
        try
        {
            var created = await _commandService.CreateCategoryAsync(NewCategoryName, string.Empty).ConfigureAwait(true);
            ActionErrorMessage = null;
            HasActionError = false;

            NewCategoryName = string.Empty;
            Categories.Add(created);
        }
#pragma warning disable CA1031 // Command boundary: a failed write must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(AddCategoryAsync));
        }
    }

    private async Task AddSupplierAsync()
    {
        try
        {
            var created = await _commandService.CreateSupplierAsync(new CreateSupplierRequest(NewSupplierName, string.Empty, string.Empty, string.Empty)).ConfigureAwait(true);
            ActionErrorMessage = null;
            HasActionError = false;

            NewSupplierName = string.Empty;
            Suppliers.Add(created);
        }
#pragma warning disable CA1031 // Command boundary: a failed write must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(AddSupplierAsync));
        }
    }
}
