using Rojan.Desktop.Application.Inventory;
using Rojan.Desktop.Application.Tests.Organizations;
using DomainInventory = Rojan.Desktop.Domain.Inventory;

namespace Rojan.Desktop.Application.Tests.Inventory;

public sealed class InventoryCommandServiceTests
{
    [Fact]
    public async Task CreateProductAsync_ValidRequest_CreatesProductAndInitialInventoryItem()
    {
        var repository = new StubInventoryRepository();
        var sut = new InventoryCommandService(repository, new StubEnterpriseContext());
        var request = new CreateProductRequest(
            "SKU-1", "Hydrating Shampoo 1L", "category-1", "Hair Care", "supplier-1", "Glow Beauty Supply Co.",
            "$18", "Sulfate-free hydrating shampoo.", 20, 5);

        var created = await sut.CreateProductAsync(request);

        Assert.Equal("Hydrating Shampoo 1L", created.Name);
        Assert.Equal(ProductStatus.Active, created.Status);
        Assert.Single(repository.Products);
        var inventoryItem = Assert.Single(repository.InventoryItems);
        Assert.Equal(created.Id, inventoryItem.ProductId);
        Assert.Equal(20, inventoryItem.QuantityOnHand);
        Assert.Equal(5, inventoryItem.ReorderThreshold);
    }

    [Fact]
    public async Task CreateCategoryAsync_ValidRequest_AddsCategory()
    {
        var repository = new StubInventoryRepository();
        var sut = new InventoryCommandService(repository, new StubEnterpriseContext());

        var created = await sut.CreateCategoryAsync("Hair Care", "Shampoos and conditioners.");

        Assert.Equal("Hair Care", created.Name);
        Assert.Single(repository.Categories);
    }

    [Fact]
    public async Task CreateSupplierAsync_ValidRequest_AddsSupplierAsActive()
    {
        var repository = new StubInventoryRepository();
        var sut = new InventoryCommandService(repository, new StubEnterpriseContext());
        var request = new CreateSupplierRequest("Glow Beauty Supply Co.", "Maria Gonzalez", "orders@example.com", "+1 555 0100");

        var created = await sut.CreateSupplierAsync(request);

        Assert.Equal(SupplierStatus.Active, created.Status);
        Assert.Single(repository.Suppliers);
    }

    [Fact]
    public async Task RecordStockTransactionAsync_Received_IncreasesQuantityAndAppendsTransaction()
    {
        var repository = new StubInventoryRepository();
        repository.InventoryItems.Add(new DomainInventory.InventoryItem("item-1", "product-1", "Test Product", 10, 5));
        var sut = new InventoryCommandService(repository, new StubEnterpriseContext());

        var transaction = await sut.RecordStockTransactionAsync("product-1", StockTransactionType.Received, 15, "Restock delivery.");

        Assert.Equal(15, transaction.Quantity);
        Assert.Equal(25, repository.InventoryItems.Single().QuantityOnHand);
        Assert.Single(repository.Transactions);
    }

    [Fact]
    public async Task RecordStockTransactionAsync_Sold_DecreasesQuantity()
    {
        var repository = new StubInventoryRepository();
        repository.InventoryItems.Add(new DomainInventory.InventoryItem("item-1", "product-1", "Test Product", 10, 5));
        var sut = new InventoryCommandService(repository, new StubEnterpriseContext());

        await sut.RecordStockTransactionAsync("product-1", StockTransactionType.Sold, 4, string.Empty);

        Assert.Equal(6, repository.InventoryItems.Single().QuantityOnHand);
    }

    [Fact]
    public async Task RecordStockTransactionAsync_InvalidQuantityForType_ThrowsArgumentExceptionAndWritesNothing()
    {
        var repository = new StubInventoryRepository();
        repository.InventoryItems.Add(new DomainInventory.InventoryItem("item-1", "product-1", "Test Product", 10, 5));
        var sut = new InventoryCommandService(repository, new StubEnterpriseContext());

        await Assert.ThrowsAsync<ArgumentException>(() => sut.RecordStockTransactionAsync("product-1", StockTransactionType.Received, 0, string.Empty));

        Assert.Equal(10, repository.InventoryItems.Single().QuantityOnHand);
        Assert.Empty(repository.Transactions);
    }

    [Fact]
    public async Task RecordStockTransactionAsync_NoInventoryRecord_ThrowsInvalidOperationException()
    {
        var repository = new StubInventoryRepository();
        var sut = new InventoryCommandService(repository, new StubEnterpriseContext());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RecordStockTransactionAsync("no-such-product", StockTransactionType.Received, 5, string.Empty));
    }

    [Fact]
    public async Task MapProductToServiceAsync_KnownProduct_AddsMapping()
    {
        var repository = new StubInventoryRepository();
        repository.Products.Add(new DomainInventory.Product("product-1", "SKU-1", "Test Product", "category-1", "Hair Care", "supplier-1", "Glow",
            "$18", DomainInventory.ProductStatus.Active, string.Empty, "org-1", "branch-1"));
        var sut = new InventoryCommandService(repository, new StubEnterpriseContext());

        var mapping = await sut.MapProductToServiceAsync("product-1", "Haircut & Style", 2);

        Assert.Equal("Haircut & Style", mapping.ServiceName);
        Assert.Equal("Test Product", mapping.ProductName);
        Assert.Equal(2, mapping.QuantityPerService);
        Assert.Single(repository.ServiceMappings);
    }

    [Fact]
    public async Task MapProductToServiceAsync_UnknownProduct_ThrowsInvalidOperationException()
    {
        var repository = new StubInventoryRepository();
        var sut = new InventoryCommandService(repository, new StubEnterpriseContext());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.MapProductToServiceAsync("no-such-product", "Haircut & Style", 1));
    }

    [Fact]
    public async Task UnmapProductFromServiceAsync_ExistingMapping_RemovesMapping()
    {
        var repository = new StubInventoryRepository();
        repository.ServiceMappings.Add(new DomainInventory.ServiceProductMapping("mapping-1", "service-1", "Haircut & Style", "product-1", "Test Product", 1));
        var sut = new InventoryCommandService(repository, new StubEnterpriseContext());

        await sut.UnmapProductFromServiceAsync("product-1", "mapping-1");

        Assert.Empty(repository.ServiceMappings);
    }
}
