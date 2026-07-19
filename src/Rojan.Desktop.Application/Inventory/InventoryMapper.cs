using DomainInventory = Rojan.Desktop.Domain.Inventory;

namespace Rojan.Desktop.Application.Inventory;

/// <summary>Domain&lt;-&gt;Application mapping shared by every Inventory use case - same reasoning as <c>Customers.CustomerMapper</c>.</summary>
internal static class InventoryMapper
{
    public static ProductDto MapProduct(DomainInventory.Product product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.CategoryId,
        product.CategoryName,
        product.SupplierId,
        product.SupplierName,
        product.UnitPrice,
        MapStatus(product.Status),
        product.Description);

    public static ProductCategoryDto MapCategory(DomainInventory.ProductCategory category) =>
        new(category.Id, category.Name, category.Description);

    public static SupplierDto MapSupplier(DomainInventory.Supplier supplier) => new(
        supplier.Id,
        supplier.Name,
        supplier.ContactName,
        supplier.Email,
        supplier.Phone,
        MapSupplierStatus(supplier.Status));

    public static InventoryItemDto MapInventoryItem(DomainInventory.InventoryItem item) =>
        new(item.Id, item.ProductId, item.ProductName, item.QuantityOnHand, item.ReorderThreshold);

    public static StockTransactionDto MapTransaction(DomainInventory.StockTransaction transaction) => new(
        transaction.Id,
        transaction.ProductId,
        transaction.ProductName,
        MapTransactionType(transaction.Type),
        transaction.Quantity,
        transaction.OccurredAt,
        transaction.Notes);

    public static ServiceProductMappingDto MapServiceMapping(DomainInventory.ServiceProductMapping mapping) =>
        new(mapping.Id, mapping.ServiceId, mapping.ServiceName, mapping.ProductId, mapping.ProductName, mapping.QuantityPerService);

    public static ProductStatus MapStatus(DomainInventory.ProductStatus status) => status switch
    {
        DomainInventory.ProductStatus.Active => ProductStatus.Active,
        DomainInventory.ProductStatus.Discontinued => ProductStatus.Discontinued,
        DomainInventory.ProductStatus.OutOfStock => ProductStatus.OutOfStock,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown domain product status."),
    };

    public static DomainInventory.ProductStatus MapStatusToDomain(ProductStatus status) => status switch
    {
        ProductStatus.Active => DomainInventory.ProductStatus.Active,
        ProductStatus.Discontinued => DomainInventory.ProductStatus.Discontinued,
        ProductStatus.OutOfStock => DomainInventory.ProductStatus.OutOfStock,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown application product status."),
    };

    public static SupplierStatus MapSupplierStatus(DomainInventory.SupplierStatus status) => status switch
    {
        DomainInventory.SupplierStatus.Active => SupplierStatus.Active,
        DomainInventory.SupplierStatus.Inactive => SupplierStatus.Inactive,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown domain supplier status."),
    };

    public static StockTransactionType MapTransactionType(DomainInventory.StockTransactionType type) => type switch
    {
        DomainInventory.StockTransactionType.Received => StockTransactionType.Received,
        DomainInventory.StockTransactionType.Sold => StockTransactionType.Sold,
        DomainInventory.StockTransactionType.Returned => StockTransactionType.Returned,
        DomainInventory.StockTransactionType.Damaged => StockTransactionType.Damaged,
        DomainInventory.StockTransactionType.Adjustment => StockTransactionType.Adjustment,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown domain stock transaction type."),
    };

    public static DomainInventory.StockTransactionType MapTransactionTypeToDomain(StockTransactionType type) => type switch
    {
        StockTransactionType.Received => DomainInventory.StockTransactionType.Received,
        StockTransactionType.Sold => DomainInventory.StockTransactionType.Sold,
        StockTransactionType.Returned => DomainInventory.StockTransactionType.Returned,
        StockTransactionType.Damaged => DomainInventory.StockTransactionType.Damaged,
        StockTransactionType.Adjustment => DomainInventory.StockTransactionType.Adjustment,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown application stock transaction type."),
    };
}
