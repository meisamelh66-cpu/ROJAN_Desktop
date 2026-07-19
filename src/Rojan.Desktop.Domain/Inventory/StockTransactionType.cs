namespace Rojan.Desktop.Domain.Inventory;

/// <summary>The kind of movement a <see cref="StockTransaction"/> represents - see <see cref="StockTransactionRules.Apply"/> for how each affects an <see cref="InventoryItem"/>'s on-hand quantity.</summary>
public enum StockTransactionType
{
    Received,
    Sold,
    Returned,
    Damaged,
    Adjustment,
}
