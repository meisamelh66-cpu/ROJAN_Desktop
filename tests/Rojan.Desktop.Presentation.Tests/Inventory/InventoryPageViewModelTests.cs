using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Inventory;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Inventory;

namespace Rojan.Desktop.Presentation.Tests.Inventory;

public sealed class InventoryPageViewModelTests
{
    private static ProductDto MakeProduct(string id, string name) =>
        new(id, "SKU-" + id, name, "category-1", "Hair Care", "supplier-1", "Glow Beauty Supply Co.", "$18", ProductStatus.Active, string.Empty, "org-1", "branch-1");

    /// <summary>A profile query stub that never fails, used by tests that don't assert on Profile - Profile is constructed as a side effect of selection, and its own errors are contained internally.</summary>
    private static StubProductProfileQueryService MakeProfileQueryService() =>
        new((productId, _) => Task.FromResult(new ProductProfileDto(MakeProduct(productId, "Placeholder"), null, [], [])));

    private static InventoryPageViewModel MakeSut(
        StubProductQueryService queryService,
        StubInventoryQueryService? inventoryQueryService = null,
        StubInventoryCommandService? commandService = null,
        RecordingLogger<InventoryPageViewModel>? logger = null) =>
        new(queryService, MakeProfileQueryService(), inventoryQueryService ?? new StubInventoryQueryService(), commandService ?? new StubInventoryCommandService(), logger);

    [Fact]
    public void Constructor_QueryServiceStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<ProductDto>>();
        var queryService = new StubProductQueryService(_ => tcs.Task);

        var sut = MakeSut(queryService);

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsProducts_StateIsLoadedAndPopulatesProducts()
    {
        var products = new List<ProductDto> { MakeProduct("product-1", "Hydrating Shampoo 1L") };
        var queryService = new StubProductQueryService(_ => Task.FromResult<IReadOnlyList<ProductDto>>(products));

        var sut = MakeSut(queryService);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal(products, sut.Products);
        Assert.Equal(products[0], sut.SelectedProduct);
        Assert.NotNull(sut.Profile);
    }

    [Fact]
    public void Constructor_QueryServiceReturnsEmptyList_StateIsEmpty()
    {
        var queryService = new StubProductQueryService(_ => Task.FromResult<IReadOnlyList<ProductDto>>([]));

        var sut = MakeSut(queryService);

        Assert.Equal(DashboardState.Empty, sut.State);
        Assert.Null(sut.SelectedProduct);
        Assert.Null(sut.Profile);
    }

    [Fact]
    public void Constructor_QueryServiceThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubProductQueryService(
            _ => Task.FromException<IReadOnlyList<ProductDto>>(new InvalidOperationException("boom")));

        var sut = MakeSut(queryService);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
    }

    // Phase 8.19 Logging Wave 2A: LoadAsync / SearchAsync now log at Error before
    // their existing handling - user-visible behaviour unchanged.

    [Fact]
    public void LoadAsync_QueryServiceThrows_LogsError()
    {
        var queryService = new StubProductQueryService(
            _ => Task.FromException<IReadOnlyList<ProductDto>>(new InvalidOperationException("boom")));
        var logger = new RecordingLogger<InventoryPageViewModel>();

        var sut = MakeSut(queryService, logger: logger);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("LoadAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows()
    {
        var queryService = new StubProductQueryService(
            _ => Task.FromException<IReadOnlyList<ProductDto>>(new InvalidOperationException("boom")));

        var exception = Record.Exception(() => MakeSut(queryService));

        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_LoadsLowStockCountFromInventoryQueryService()
    {
        var products = new List<ProductDto> { MakeProduct("product-1", "Hydrating Shampoo 1L") };
        var queryService = new StubProductQueryService(_ => Task.FromResult<IReadOnlyList<ProductDto>>(products));
        var inventoryQueryService = new StubInventoryQueryService(
            _ => Task.FromResult<IReadOnlyList<InventoryItemDto>>([
                new InventoryItemDto("item-1", "product-1", "Hydrating Shampoo 1L", 3, 10),
            ]));

        var sut = MakeSut(queryService, inventoryQueryService);

        Assert.Equal(1, sut.LowStockCount);
    }

    [Fact]
    public void SearchText_MatchesName_FiltersToMatchingProductsOnly()
    {
        var products = new List<ProductDto>
        {
            MakeProduct("product-1", "Hydrating Shampoo 1L"),
            MakeProduct("product-2", "Gel Polish - Rose Quartz"),
        };
        var queryService = new StubProductQueryService(_ => Task.FromResult<IReadOnlyList<ProductDto>>(products));
        var sut = MakeSut(queryService);

        sut.SearchText = "gel polish";

        Assert.Equal(["product-2"], sut.Products.Select(p => p.Id));
    }

    [Fact]
    public void LoadCommand_ExecutedAfterFailure_RecoversToLoadedState()
    {
        var shouldFail = true;
        var products = new List<ProductDto> { MakeProduct("product-1", "Hydrating Shampoo 1L") };
        var queryService = new StubProductQueryService(_ => shouldFail
            ? Task.FromException<IReadOnlyList<ProductDto>>(new InvalidOperationException("boom"))
            : Task.FromResult<IReadOnlyList<ProductDto>>(products));
        var sut = MakeSut(queryService);
        Assert.Equal(DashboardState.Error, sut.State);

        shouldFail = false;
        sut.LoadCommand.Execute(null);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Null(sut.ErrorMessage);
        Assert.Equal(products, sut.Products);
    }

    [Fact]
    public void CreateProductCommand_RequiredFieldsMissing_CanExecuteIsFalse()
    {
        var queryService = new StubProductQueryService(_ => Task.FromResult<IReadOnlyList<ProductDto>>([]));
        var sut = MakeSut(queryService);

        Assert.False(sut.CreateProductCommand.CanExecute(null));

        sut.NewProductSku = "SKU-NEW";
        sut.NewProductName = "New Product";
        sut.SelectedNewProductCategory = new ProductCategoryDto("category-1", "Hair Care", string.Empty);
        sut.SelectedNewProductSupplier = new SupplierDto("supplier-1", "Glow Beauty Supply Co.", string.Empty, string.Empty, string.Empty, SupplierStatus.Active);

        Assert.True(sut.CreateProductCommand.CanExecute(null));
    }

    [Fact]
    public void CreateProductCommand_Executed_CallsCommandServiceReloadsListAndSelectsNewProduct()
    {
        var existing = new List<ProductDto> { MakeProduct("product-1", "Hydrating Shampoo 1L") };
        var queryService = new StubProductQueryService(_ => Task.FromResult<IReadOnlyList<ProductDto>>(existing.ToList()));
        var commandService = new StubInventoryCommandService();
        var sut = MakeSut(queryService, commandService: commandService);
        sut.NewProductSku = "SKU-NEW";
        sut.NewProductName = "New Product";
        sut.SelectedNewProductCategory = new ProductCategoryDto("category-1", "Hair Care", string.Empty);
        sut.SelectedNewProductSupplier = new SupplierDto("supplier-1", "Glow Beauty Supply Co.", string.Empty, string.Empty, string.Empty, SupplierStatus.Active);

        sut.CreateProductCommand.Execute(null);

        var request = Assert.Single(commandService.CreateProductRequests);
        Assert.Equal("New Product", request.Name);
        Assert.Equal(string.Empty, sut.NewProductSku);
    }

    [Fact]
    public void AddCategoryCommand_Executed_CallsCommandServiceAndAppendsToCategories()
    {
        var queryService = new StubProductQueryService(_ => Task.FromResult<IReadOnlyList<ProductDto>>([]));
        var commandService = new StubInventoryCommandService();
        var sut = MakeSut(queryService, commandService: commandService);
        sut.NewCategoryName = "Spa & Wellness";

        sut.AddCategoryCommand.Execute(null);

        var call = Assert.Single(commandService.CreateCategoryCalls);
        Assert.Equal("Spa & Wellness", call.Name);
        Assert.Contains(sut.Categories, c => c.Name == "Spa & Wellness");
        Assert.Equal(string.Empty, sut.NewCategoryName);
    }

    [Fact]
    public void AddSupplierCommand_Executed_CallsCommandServiceAndAppendsToSuppliers()
    {
        var queryService = new StubProductQueryService(_ => Task.FromResult<IReadOnlyList<ProductDto>>([]));
        var commandService = new StubInventoryCommandService();
        var sut = MakeSut(queryService, commandService: commandService);
        sut.NewSupplierName = "Northline Wholesale";

        sut.AddSupplierCommand.Execute(null);

        var call = Assert.Single(commandService.CreateSupplierRequests);
        Assert.Equal("Northline Wholesale", call.Name);
        Assert.Contains(sut.Suppliers, s => s.Name == "Northline Wholesale");
        Assert.Equal(string.Empty, sut.NewSupplierName);
    }

    // ---------------------------------------------------------------------
    // Production Hardening - Missing-Guard Sweep Wave C (Inventory commands).
    // Create-product / add-category / add-supplier now surface a backend
    // failure via the non-destructive ActionErrorMessage/HasActionError pair
    // instead of the global dialog. Failures never expose SKU / cost /
    // supplier / stock values, and log operation-name-only.
    // ---------------------------------------------------------------------

    private const string InventoryBackendSecret = "backend 500: SKU=WIDGET-9 cost=42.50 supplier=Acme Corp on-hand=7";

    private static void FillNewProductForm(InventoryPageViewModel sut)
    {
        sut.NewProductSku = "SKU-NEW";
        sut.NewProductName = "New Product";
        sut.SelectedNewProductCategory = new ProductCategoryDto("category-1", "Hair Care", string.Empty);
        sut.SelectedNewProductSupplier = new SupplierDto("supplier-1", "Glow Beauty Supply Co.", string.Empty, string.Empty, string.Empty, SupplierStatus.Active);
    }

    [Fact]
    public void CreateProductCommand_Failure_DoesNotThrow_SetsActionErrorAndPreservesForm()
    {
        var queryService = new StubProductQueryService(_ => Task.FromResult<IReadOnlyList<ProductDto>>([]));
        var commandService = new StubInventoryCommandService { CreateProductException = new InvalidOperationException(InventoryBackendSecret) };
        var sut = MakeSut(queryService, commandService: commandService);
        FillNewProductForm(sut);

        var exception = Record.Exception(() => sut.CreateProductCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.NotEqual(DashboardState.Error, sut.State);
        Assert.Equal("SKU-NEW", sut.NewProductSku);
        Assert.Equal("New Product", sut.NewProductName);
        Assert.Single(commandService.CreateProductRequests);
    }

    [Fact]
    public void AddCategoryCommand_Failure_DoesNotThrow_SetsActionErrorAndDoesNotAppend()
    {
        var queryService = new StubProductQueryService(_ => Task.FromResult<IReadOnlyList<ProductDto>>([]));
        var commandService = new StubInventoryCommandService { CreateCategoryException = new InvalidOperationException(InventoryBackendSecret) };
        var sut = MakeSut(queryService, commandService: commandService);
        sut.NewCategoryName = "Spa & Wellness";

        var exception = Record.Exception(() => sut.AddCategoryCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.DoesNotContain(sut.Categories, c => c.Name == "Spa & Wellness");
        Assert.Equal("Spa & Wellness", sut.NewCategoryName);
    }

    [Fact]
    public void AddSupplierCommand_Failure_DoesNotThrow_SetsActionErrorAndDoesNotAppend()
    {
        var queryService = new StubProductQueryService(_ => Task.FromResult<IReadOnlyList<ProductDto>>([]));
        var commandService = new StubInventoryCommandService { CreateSupplierException = new InvalidOperationException(InventoryBackendSecret) };
        var sut = MakeSut(queryService, commandService: commandService);
        sut.NewSupplierName = "Northline Wholesale";

        var exception = Record.Exception(() => sut.AddSupplierCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.DoesNotContain(sut.Suppliers, s => s.Name == "Northline Wholesale");
        Assert.Equal("Northline Wholesale", sut.NewSupplierName);
    }

    [Fact]
    public void CreateProductCommand_Failure_LogsOperationNameOnly_NoSkuOrCostLeak()
    {
        var queryService = new StubProductQueryService(_ => Task.FromResult<IReadOnlyList<ProductDto>>([]));
        var commandService = new StubInventoryCommandService { CreateProductException = new InvalidOperationException(InventoryBackendSecret) };
        var logger = new RecordingLogger<InventoryPageViewModel>();
        var sut = MakeSut(queryService, commandService: commandService, logger: logger);
        FillNewProductForm(sut);

        sut.CreateProductCommand.Execute(null);

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("Operation=CreateProductAsync", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains(InventoryBackendSecret, StringComparison.Ordinal));
        Assert.DoesNotContain(InventoryBackendSecret, sut.ActionErrorMessage ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateProductCommand_SuccessAfterFailure_ClearsActionError()
    {
        var queryService = new StubProductQueryService(_ => Task.FromResult<IReadOnlyList<ProductDto>>([]));
        var commandService = new StubInventoryCommandService { CreateProductException = new InvalidOperationException("boom") };
        var sut = MakeSut(queryService, commandService: commandService);
        FillNewProductForm(sut);
        sut.CreateProductCommand.Execute(null);
        Assert.True(sut.HasActionError);

        commandService.CreateProductException = null;
        FillNewProductForm(sut);
        sut.CreateProductCommand.Execute(null);

        Assert.False(sut.HasActionError);
        Assert.Null(sut.ActionErrorMessage);
        Assert.Equal(string.Empty, sut.NewProductSku);
    }

    [Fact]
    public void LoggerFactory_ForwardedToProfileChild_ChildLoadFailureIsLoggedViaTheFactory()
    {
        var products = new List<ProductDto> { MakeProduct("product-1", "Hydrating Shampoo 1L") };
        var queryService = new StubProductQueryService(_ => Task.FromResult<IReadOnlyList<ProductDto>>(products));
        var failingProfileQuery = new StubProductProfileQueryService((_, _) => Task.FromException<ProductProfileDto>(new InvalidOperationException("child boom")));
        var loggerFactory = new RecordingLoggerFactory();

        var sut = new InventoryPageViewModel(queryService, failingProfileQuery, new StubInventoryQueryService(), new StubInventoryCommandService(), logger: null, loggerFactory: loggerFactory);

        Assert.NotNull(sut.Profile);
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains(nameof(InventoryProfileViewModel), entry.Category, StringComparison.Ordinal);
        Assert.Contains("Operation=LoadAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("child boom", entry.Message, StringComparison.Ordinal);
    }
}
