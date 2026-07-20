using Rojan.Desktop.Application.Inventory;
using DomainInventory = Rojan.Desktop.Domain.Inventory;

namespace Rojan.Desktop.Application.Tests.Inventory;

public sealed class ProductProfileQueryServiceTests
{
    private static DomainInventory.Product MakeProduct(string id = "product-1") =>
        new(id, "SKU-1", "Hydrating Shampoo 1L", "category-1", "Hair Care", "supplier-1", "Glow Beauty Supply Co.",
            "$18", DomainInventory.ProductStatus.Active, "Sulfate-free hydrating shampoo.", "org-1", "branch-1");

    [Fact]
    public async Task GetProfileAsync_KnownProduct_AssemblesFullAggregate()
    {
        var product = MakeProduct();
        var repository = new StubInventoryRepository([product]);
        repository.InventoryItems.Add(new DomainInventory.InventoryItem("item-1", "product-1", product.Name, 42, 15));
        repository.Transactions.Add(new DomainInventory.StockTransaction("txn-1", "product-1", product.Name, DomainInventory.StockTransactionType.Received, 42, DateTimeOffset.UnixEpoch, string.Empty));
        repository.ServiceMappings.Add(new DomainInventory.ServiceProductMapping("mapping-1", "service-1", "Haircut & Style", "product-1", product.Name, 1));
        var sut = new ProductProfileQueryService(repository);

        var profile = await sut.GetProfileAsync("product-1");

        Assert.Equal("product-1", profile.Product.Id);
        Assert.NotNull(profile.Stock);
        Assert.Equal(42, profile.Stock!.QuantityOnHand);
        Assert.Single(profile.RecentTransactions);
        Assert.Single(profile.ServiceMappings);
    }

    [Fact]
    public async Task GetProfileAsync_NoInventoryRecord_StockIsNull()
    {
        var product = MakeProduct();
        var repository = new StubInventoryRepository([product]);
        var sut = new ProductProfileQueryService(repository);

        var profile = await sut.GetProfileAsync("product-1");

        Assert.Null(profile.Stock);
    }

    [Fact]
    public async Task GetProfileAsync_UnknownProduct_ThrowsInvalidOperationException()
    {
        var repository = new StubInventoryRepository([]);
        var sut = new ProductProfileQueryService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetProfileAsync("no-such-product"));
    }
}
