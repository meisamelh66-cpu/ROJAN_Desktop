namespace Rojan.Desktop.Domain.Inventory;

/// <summary>
/// Repository abstraction for the Inventory vertical slice - the widest
/// repository interface in this app since this slice covers six related
/// aggregate types (Product, ProductCategory, Supplier, InventoryItem,
/// StockTransaction, ServiceProductMapping) rather than one or two;
/// still a single interface per slice, same convention every other
/// module follows. Domain defines the contract; Infrastructure provides
/// the concrete implementation (a fake/in-memory one for now - Phase 17
/// explicitly has no backend integration yet, same as every other
/// vertical slice in this app). Deliberately "dumb" - quantity
/// arithmetic (how a StockTransaction affects on-hand quantity) is
/// Application's job (<c>InventoryCommandService</c>, composing
/// <see cref="StockTransactionRules"/>), not this repository's.
/// </summary>
public interface IInventoryRepository
{
    public Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default);

    public Task<Product?> GetProductByIdAsync(string productId, CancellationToken cancellationToken = default);

    public Task<Product> CreateProductAsync(Product product, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    public Task<ProductCategory> CreateCategoryAsync(ProductCategory category, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<Supplier>> GetSuppliersAsync(CancellationToken cancellationToken = default);

    public Task<Supplier> CreateSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<InventoryItem>> GetInventoryItemsAsync(CancellationToken cancellationToken = default);

    public Task<InventoryItem?> GetInventoryItemByProductIdAsync(string productId, CancellationToken cancellationToken = default);

    public Task<InventoryItem> CreateInventoryItemAsync(InventoryItem item, CancellationToken cancellationToken = default);

    public Task<InventoryItem> UpdateInventoryQuantityAsync(string productId, int quantityOnHand, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<StockTransaction>> GetTransactionsForProductAsync(string productId, CancellationToken cancellationToken = default);

    public Task<StockTransaction> RecordTransactionAsync(StockTransaction transaction, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ServiceProductMapping>> GetServiceMappingsForProductAsync(string productId, CancellationToken cancellationToken = default);

    public Task<ServiceProductMapping> MapProductToServiceAsync(ServiceProductMapping mapping, CancellationToken cancellationToken = default);

    public Task UnmapProductFromServiceAsync(string productId, string mappingId, CancellationToken cancellationToken = default);
}
