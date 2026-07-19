namespace Rojan.Desktop.Domain.Inventory;

/// <summary>
/// A product's current stock level, as returned by <see cref="IInventoryRepository"/>.
/// One per <see cref="Product"/> - <see cref="ReorderThreshold"/> is the
/// business input that drives low-stock monitoring (see
/// <c>Application.Inventory.IInventoryQueryService.GetLowStockItemsAsync</c>),
/// compared against <see cref="QuantityOnHand"/>.
/// </summary>
public sealed record InventoryItem(string Id, string ProductId, string ProductName, int QuantityOnHand, int ReorderThreshold);
