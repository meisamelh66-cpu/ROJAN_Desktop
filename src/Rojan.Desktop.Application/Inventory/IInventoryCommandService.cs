namespace Rojan.Desktop.Application.Inventory;

/// <summary>
/// Write use cases for Inventory - product/category/supplier creation,
/// stock transactions, and service-to-product mapping, all through one
/// command service, same "one command service per slice" convention as
/// Customers/Specialists.
/// </summary>
public interface IInventoryCommandService
{
    public Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    public Task<ProductCategoryDto> CreateCategoryAsync(string name, string description, CancellationToken cancellationToken = default);

    public Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);

    /// <summary>Records a stock transaction and applies it to the product's on-hand quantity (see <c>Domain.Inventory.StockTransactionRules.Apply</c>) - the two writes that together are "recording a stock transaction".</summary>
    public Task<StockTransactionDto> RecordStockTransactionAsync(string productId, StockTransactionType type, int quantity, string notes, CancellationToken cancellationToken = default);

    public Task<ServiceProductMappingDto> MapProductToServiceAsync(string productId, string serviceName, int quantityPerService, CancellationToken cancellationToken = default);

    public Task UnmapProductFromServiceAsync(string productId, string mappingId, CancellationToken cancellationToken = default);
}
