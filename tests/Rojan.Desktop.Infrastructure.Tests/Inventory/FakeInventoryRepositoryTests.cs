using Rojan.Desktop.Domain.Inventory;
using Rojan.Desktop.Infrastructure.Inventory;

namespace Rojan.Desktop.Infrastructure.Tests.Inventory;

/// <summary>Smoke + behavioral coverage - same reasoning as Customers.FakeCustomerRepositoryTests, covering all six seeded Inventory aggregate types.</summary>
public sealed class FakeInventoryRepositoryTests
{
    [Fact]
    public async Task GetProductsAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeInventoryRepository();

        var result = await sut.GetProductsAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetProductsAsync_CancellationAlreadyRequested_ThrowsTaskCanceledException()
    {
        var sut = new FakeInventoryRepository();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => sut.GetProductsAsync(cts.Token));
    }

    [Fact]
    public async Task GetProductByIdAsync_KnownId_ReturnsMatchingProduct()
    {
        var sut = new FakeInventoryRepository();

        var product = await sut.GetProductByIdAsync("product-1");

        Assert.NotNull(product);
        Assert.Equal("product-1", product.Id);
    }

    [Fact]
    public async Task GetProductByIdAsync_UnknownId_ReturnsNull()
    {
        var sut = new FakeInventoryRepository();

        var product = await sut.GetProductByIdAsync("no-such-product");

        Assert.Null(product);
    }

    [Fact]
    public async Task CreateProductAsync_NewProduct_BecomesVisibleViaGetProductsAsync()
    {
        var sut = new FakeInventoryRepository();
        var product = new Product("product-new", "SKU-NEW", "New Product", "category-1", "Hair Care", "supplier-1", "Glow Beauty Supply Co.",
            "$10", ProductStatus.Active, string.Empty, "org-1", "branch-1");

        await sut.CreateProductAsync(product);
        var products = await sut.GetProductsAsync();

        Assert.Contains(products, p => p.Id == "product-new");
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeInventoryRepository();

        var result = await sut.GetCategoriesAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task CreateCategoryAsync_NewCategory_BecomesVisibleViaGetCategoriesAsync()
    {
        var sut = new FakeInventoryRepository();
        var category = new ProductCategory("category-new", "New Category", string.Empty);

        await sut.CreateCategoryAsync(category);
        var categories = await sut.GetCategoriesAsync();

        Assert.Contains(categories, c => c.Id == "category-new");
    }

    [Fact]
    public async Task GetSuppliersAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeInventoryRepository();

        var result = await sut.GetSuppliersAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task CreateSupplierAsync_NewSupplier_BecomesVisibleViaGetSuppliersAsync()
    {
        var sut = new FakeInventoryRepository();
        var supplier = new Supplier("supplier-new", "New Supplier", string.Empty, string.Empty, string.Empty, SupplierStatus.Active);

        await sut.CreateSupplierAsync(supplier);
        var suppliers = await sut.GetSuppliersAsync();

        Assert.Contains(suppliers, s => s.Id == "supplier-new");
    }

    [Fact]
    public async Task GetInventoryItemsAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeInventoryRepository();

        var result = await sut.GetInventoryItemsAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetInventoryItemByProductIdAsync_KnownProduct_ReturnsMatchingItem()
    {
        var sut = new FakeInventoryRepository();

        var item = await sut.GetInventoryItemByProductIdAsync("product-1");

        Assert.NotNull(item);
        Assert.Equal("product-1", item.ProductId);
    }

    [Fact]
    public async Task CreateInventoryItemAsync_NewItem_BecomesVisibleViaGetInventoryItemByProductIdAsync()
    {
        var sut = new FakeInventoryRepository();
        var item = new InventoryItem("item-new", "product-new", "New Product", 5, 2);

        await sut.CreateInventoryItemAsync(item);
        var reloaded = await sut.GetInventoryItemByProductIdAsync("product-new");

        Assert.NotNull(reloaded);
        Assert.Equal(5, reloaded.QuantityOnHand);
    }

    [Fact]
    public async Task UpdateInventoryQuantityAsync_ExistingProduct_ChangesQuantity()
    {
        var sut = new FakeInventoryRepository();

        var updated = await sut.UpdateInventoryQuantityAsync("product-1", 99);
        var reloaded = await sut.GetInventoryItemByProductIdAsync("product-1");

        Assert.Equal(99, updated.QuantityOnHand);
        Assert.Equal(99, reloaded!.QuantityOnHand);
    }

    [Fact]
    public async Task UpdateInventoryQuantityAsync_UnknownProduct_ThrowsInvalidOperationException()
    {
        var sut = new FakeInventoryRepository();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateInventoryQuantityAsync("no-such-product", 5));
    }

    [Fact]
    public async Task GetTransactionsForProductAsync_KnownProduct_ReturnsOnlyThatProductsTransactions()
    {
        var sut = new FakeInventoryRepository();

        var transactions = await sut.GetTransactionsForProductAsync("product-1");

        Assert.NotEmpty(transactions);
        Assert.All(transactions, transaction => Assert.Equal("product-1", transaction.ProductId));
    }

    [Fact]
    public async Task RecordTransactionAsync_NewTransaction_BecomesVisibleViaGetTransactionsForProductAsync()
    {
        var sut = new FakeInventoryRepository();
        var transaction = new StockTransaction("txn-new", "product-1", "Hydrating Shampoo 1L", StockTransactionType.Received, 10, DateTimeOffset.Now, string.Empty);

        await sut.RecordTransactionAsync(transaction);
        var transactions = await sut.GetTransactionsForProductAsync("product-1");

        Assert.Contains(transactions, t => t.Id == "txn-new");
    }

    [Fact]
    public async Task GetServiceMappingsForProductAsync_KnownProduct_ReturnsOnlyThatProductsMappings()
    {
        var sut = new FakeInventoryRepository();

        var mappings = await sut.GetServiceMappingsForProductAsync("product-1");

        Assert.NotEmpty(mappings);
        Assert.All(mappings, mapping => Assert.Equal("product-1", mapping.ProductId));
    }

    [Fact]
    public async Task MapProductToServiceAsync_NewMapping_BecomesVisibleViaGetServiceMappingsForProductAsync()
    {
        var sut = new FakeInventoryRepository();
        var mapping = new ServiceProductMapping("mapping-new", "service-new", "New Service", "product-1", "Hydrating Shampoo 1L", 1);

        await sut.MapProductToServiceAsync(mapping);
        var mappings = await sut.GetServiceMappingsForProductAsync("product-1");

        Assert.Contains(mappings, m => m.Id == "mapping-new");
    }

    [Fact]
    public async Task UnmapProductFromServiceAsync_ExistingMapping_NoLongerReturnedByGetServiceMappingsForProductAsync()
    {
        var sut = new FakeInventoryRepository();
        var mapping = new ServiceProductMapping("mapping-new", "service-new", "New Service", "product-1", "Hydrating Shampoo 1L", 1);
        await sut.MapProductToServiceAsync(mapping);

        await sut.UnmapProductFromServiceAsync("product-1", "mapping-new");
        var mappings = await sut.GetServiceMappingsForProductAsync("product-1");

        Assert.DoesNotContain(mappings, m => m.Id == "mapping-new");
    }
}
