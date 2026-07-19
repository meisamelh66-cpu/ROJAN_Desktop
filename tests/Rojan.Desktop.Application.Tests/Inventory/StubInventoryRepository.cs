using Rojan.Desktop.Domain.Inventory;

namespace Rojan.Desktop.Application.Tests.Inventory;

/// <summary>In-memory, mutable <see cref="IInventoryRepository"/> test double - same reasoning as Customers.StubCustomerRepository, covering all six Inventory aggregate types.</summary>
internal sealed class StubInventoryRepository : IInventoryRepository
{
    public List<Product> Products { get; } = [];

    public List<ProductCategory> Categories { get; } = [];

    public List<Supplier> Suppliers { get; } = [];

    public List<InventoryItem> InventoryItems { get; } = [];

    public List<StockTransaction> Transactions { get; } = [];

    public List<ServiceProductMapping> ServiceMappings { get; } = [];

    public StubInventoryRepository()
    {
    }

    public StubInventoryRepository(IReadOnlyList<Product> products)
    {
        Products.AddRange(products);
    }

    public Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Product>>(Products.ToList());

    public Task<Product?> GetProductByIdAsync(string productId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Products.FirstOrDefault(product => product.Id == productId));

    public Task<Product> CreateProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        Products.Add(product);
        return Task.FromResult(product);
    }

    public Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProductCategory>>(Categories.ToList());

    public Task<ProductCategory> CreateCategoryAsync(ProductCategory category, CancellationToken cancellationToken = default)
    {
        Categories.Add(category);
        return Task.FromResult(category);
    }

    public Task<IReadOnlyList<Supplier>> GetSuppliersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Supplier>>(Suppliers.ToList());

    public Task<Supplier> CreateSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        Suppliers.Add(supplier);
        return Task.FromResult(supplier);
    }

    public Task<IReadOnlyList<InventoryItem>> GetInventoryItemsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<InventoryItem>>(InventoryItems.ToList());

    public Task<InventoryItem?> GetInventoryItemByProductIdAsync(string productId, CancellationToken cancellationToken = default) =>
        Task.FromResult(InventoryItems.FirstOrDefault(item => item.ProductId == productId));

    public Task<InventoryItem> CreateInventoryItemAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        InventoryItems.Add(item);
        return Task.FromResult(item);
    }

    public Task<InventoryItem> UpdateInventoryQuantityAsync(string productId, int quantityOnHand, CancellationToken cancellationToken = default)
    {
        var index = InventoryItems.FindIndex(item => item.ProductId == productId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Product '{productId}' has no inventory record.");
        }

        var updated = InventoryItems[index] with { QuantityOnHand = quantityOnHand };
        InventoryItems[index] = updated;
        return Task.FromResult(updated);
    }

    public Task<IReadOnlyList<StockTransaction>> GetTransactionsForProductAsync(string productId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StockTransaction>>(Transactions.Where(transaction => transaction.ProductId == productId).ToList());

    public Task<StockTransaction> RecordTransactionAsync(StockTransaction transaction, CancellationToken cancellationToken = default)
    {
        Transactions.Add(transaction);
        return Task.FromResult(transaction);
    }

    public Task<IReadOnlyList<ServiceProductMapping>> GetServiceMappingsForProductAsync(string productId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceProductMapping>>(ServiceMappings.Where(mapping => mapping.ProductId == productId).ToList());

    public Task<ServiceProductMapping> MapProductToServiceAsync(ServiceProductMapping mapping, CancellationToken cancellationToken = default)
    {
        ServiceMappings.Add(mapping);
        return Task.FromResult(mapping);
    }

    public Task UnmapProductFromServiceAsync(string productId, string mappingId, CancellationToken cancellationToken = default)
    {
        ServiceMappings.RemoveAll(mapping => mapping.ProductId == productId && mapping.Id == mappingId);
        return Task.CompletedTask;
    }
}
