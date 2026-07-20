using Rojan.Desktop.Application.Inventory;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Inventory;

namespace Rojan.Desktop.Presentation.Tests.Inventory;

public sealed class InventoryProfileViewModelTests
{
    private static ProductProfileDto MakeProfile(string productId = "product-1") =>
        new(
            new ProductDto(productId, "SKU-1", "Hydrating Shampoo 1L", "category-1", "Hair Care", "supplier-1", "Glow Beauty Supply Co.", "$18", ProductStatus.Active, string.Empty, "org-1", "branch-1"),
            new InventoryItemDto("item-1", productId, "Hydrating Shampoo 1L", 42, 15),
            [new StockTransactionDto("txn-1", productId, "Hydrating Shampoo 1L", StockTransactionType.Received, 48, DateTimeOffset.UnixEpoch, string.Empty)],
            [new ServiceProductMappingDto("mapping-1", "service-1", "Haircut & Style", productId, "Hydrating Shampoo 1L", 1)]);

    [Fact]
    public void Constructor_ProfileQueryStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<ProductProfileDto>();
        var profileQuery = new StubProductProfileQueryService((_, _) => tcs.Task);
        var commandService = new StubInventoryCommandService();

        var sut = new InventoryProfileViewModel("product-1", profileQuery, commandService);

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_ProfileQueryReturnsProfile_PopulatesProductStockTransactionsAndMappings()
    {
        var profileQuery = new StubProductProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubInventoryCommandService();

        var sut = new InventoryProfileViewModel("product-1", profileQuery, commandService);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal("Hydrating Shampoo 1L", sut.Product?.Name);
        Assert.Equal(42, sut.Stock?.QuantityOnHand);
        Assert.Single(sut.RecentTransactions);
        Assert.Single(sut.ServiceMappings);
    }

    [Fact]
    public void Constructor_ProfileQueryThrows_StateIsErrorAndSetsErrorMessage()
    {
        var profileQuery = new StubProductProfileQueryService((_, _) => Task.FromException<ProductProfileDto>(new InvalidOperationException("boom")));
        var commandService = new StubInventoryCommandService();

        var sut = new InventoryProfileViewModel("product-1", profileQuery, commandService);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void RecordTransactionCommand_QuantityIsZero_CanExecuteIsFalse()
    {
        var profileQuery = new StubProductProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubInventoryCommandService();
        var sut = new InventoryProfileViewModel("product-1", profileQuery, commandService)
        {
            TransactionQuantity = 0,
        };

        Assert.False(sut.RecordTransactionCommand.CanExecute(null));

        sut.TransactionQuantity = 5;

        Assert.True(sut.RecordTransactionCommand.CanExecute(null));
    }

    [Fact]
    public void RecordTransactionCommand_Executed_CallsCommandServiceWithProductIdTypeAndQuantityThenResetsInput()
    {
        var profileQuery = new StubProductProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubInventoryCommandService();
        var sut = new InventoryProfileViewModel("product-1", profileQuery, commandService)
        {
            SelectedTransactionType = StockTransactionType.Sold,
            TransactionQuantity = 3,
            TransactionNotes = "Retail sale.",
        };

        sut.RecordTransactionCommand.Execute(null);

        var call = Assert.Single(commandService.RecordTransactionCalls);
        Assert.Equal("product-1", call.ProductId);
        Assert.Equal(StockTransactionType.Sold, call.Type);
        Assert.Equal(3, call.Quantity);
        Assert.Equal("Retail sale.", call.Notes);
        Assert.Equal(1, sut.TransactionQuantity);
        Assert.Equal(string.Empty, sut.TransactionNotes);
    }

    [Fact]
    public void MapServiceCommand_ServiceNameIsEmpty_CanExecuteIsFalse()
    {
        var profileQuery = new StubProductProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubInventoryCommandService();
        var sut = new InventoryProfileViewModel("product-1", profileQuery, commandService);

        Assert.False(sut.MapServiceCommand.CanExecute(null));

        sut.NewMappingServiceName = "Colour Touch-Up";

        Assert.True(sut.MapServiceCommand.CanExecute(null));
    }

    [Fact]
    public void MapServiceCommand_Executed_CallsCommandServiceWithProductIdServiceNameAndQuantity()
    {
        var profileQuery = new StubProductProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubInventoryCommandService();
        var sut = new InventoryProfileViewModel("product-1", profileQuery, commandService)
        {
            NewMappingServiceName = "Colour Touch-Up",
            NewMappingQuantityPerService = 2,
        };

        sut.MapServiceCommand.Execute(null);

        var call = Assert.Single(commandService.MapServiceCalls);
        Assert.Equal("product-1", call.ProductId);
        Assert.Equal("Colour Touch-Up", call.ServiceName);
        Assert.Equal(2, call.QuantityPerService);
        Assert.Equal(string.Empty, sut.NewMappingServiceName);
    }

    [Fact]
    public void UnmapServiceCommand_Executed_CallsCommandServiceWithProductIdAndMappingId()
    {
        var profileQuery = new StubProductProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubInventoryCommandService();
        var sut = new InventoryProfileViewModel("product-1", profileQuery, commandService);
        var mapping = new ServiceProductMappingDto("mapping-1", "service-1", "Haircut & Style", "product-1", "Hydrating Shampoo 1L", 1);

        sut.UnmapServiceCommand.Execute(mapping);

        var call = Assert.Single(commandService.UnmapServiceCalls);
        Assert.Equal("product-1", call.ProductId);
        Assert.Equal("mapping-1", call.MappingId);
    }
}
