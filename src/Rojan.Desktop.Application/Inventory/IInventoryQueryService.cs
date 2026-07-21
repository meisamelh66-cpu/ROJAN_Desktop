namespace Rojan.Desktop.Application.Inventory;

/// <summary>Read-only use case Presentation depends on to load stock-level data - the low-stock monitoring query, separate from <see cref="IProductQueryService"/> since it reads a different aggregate (InventoryItem, not Product).</summary>
public interface IInventoryQueryService
{
    public Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>Items whose on-hand quantity is at or below their reorder threshold - composes over <see cref="GetInventoryItemsAsync"/> rather than a dedicated repository method, same "read the set, compose in Application" convention every other module's filtering follows.</summary>
    public Task<IReadOnlyList<InventoryItemDto>> GetLowStockItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>Every stock transaction across every product, most recent first - Phase 33's Inventory Movements/Supplier Purchases reports.</summary>
    public Task<IReadOnlyList<StockTransactionDto>> GetAllTransactionsAsync(CancellationToken cancellationToken = default);
}
