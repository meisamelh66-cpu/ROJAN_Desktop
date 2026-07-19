using Rojan.Desktop.Application.Inventory;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Inventory;

namespace Rojan.Desktop.Presentation.Tests.Inventory;

public sealed class InventoryPageViewModelTests
{
    private static ProductDto MakeProduct(string id, string name) =>
        new(id, "SKU-" + id, name, "category-1", "Hair Care", "supplier-1", "Glow Beauty Supply Co.", "$18", ProductStatus.Active, string.Empty);

    /// <summary>A profile query stub that never fails, used by tests that don't assert on Profile - Profile is constructed as a side effect of selection, and its own errors are contained internally.</summary>
    private static StubProductProfileQueryService MakeProfileQueryService() =>
        new((productId, _) => Task.FromResult(new ProductProfileDto(MakeProduct(productId, "Placeholder"), null, [], [])));

    private static InventoryPageViewModel MakeSut(
        StubProductQueryService queryService,
        StubInventoryQueryService? inventoryQueryService = null,
        StubInventoryCommandService? commandService = null) =>
        new(queryService, MakeProfileQueryService(), inventoryQueryService ?? new StubInventoryQueryService(), commandService ?? new StubInventoryCommandService());

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
        Assert.Equal("boom", sut.ErrorMessage);
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
}
