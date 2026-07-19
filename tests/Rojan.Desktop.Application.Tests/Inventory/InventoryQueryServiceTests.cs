using Rojan.Desktop.Application.Inventory;
using DomainInventory = Rojan.Desktop.Domain.Inventory;

namespace Rojan.Desktop.Application.Tests.Inventory;

public sealed class InventoryQueryServiceTests
{
    [Fact]
    public async Task GetInventoryItemsAsync_RepositoryReturnsItems_MapsEveryField()
    {
        var repository = new StubInventoryRepository();
        repository.InventoryItems.Add(new DomainInventory.InventoryItem("item-1", "product-1", "Test Product", 10, 5));
        var sut = new InventoryQueryService(repository);

        var result = await sut.GetInventoryItemsAsync();

        var dto = Assert.Single(result);
        Assert.Equal(10, dto.QuantityOnHand);
        Assert.Equal(5, dto.ReorderThreshold);
    }

    [Fact]
    public async Task GetLowStockItemsAsync_MixOfLevels_ReturnsOnlyAtOrBelowThreshold()
    {
        var repository = new StubInventoryRepository();
        repository.InventoryItems.Add(new DomainInventory.InventoryItem("item-1", "product-1", "Healthy Stock", 40, 15));
        repository.InventoryItems.Add(new DomainInventory.InventoryItem("item-2", "product-2", "Low Stock", 8, 10));
        repository.InventoryItems.Add(new DomainInventory.InventoryItem("item-3", "product-3", "Exactly At Threshold", 10, 10));
        var sut = new InventoryQueryService(repository);

        var result = await sut.GetLowStockItemsAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.ProductId == "product-2");
        Assert.Contains(result, item => item.ProductId == "product-3");
    }

    [Fact]
    public async Task GetLowStockItemsAsync_NothingLow_ReturnsEmptyList()
    {
        var repository = new StubInventoryRepository();
        repository.InventoryItems.Add(new DomainInventory.InventoryItem("item-1", "product-1", "Healthy Stock", 40, 15));
        var sut = new InventoryQueryService(repository);

        var result = await sut.GetLowStockItemsAsync();

        Assert.Empty(result);
    }
}
