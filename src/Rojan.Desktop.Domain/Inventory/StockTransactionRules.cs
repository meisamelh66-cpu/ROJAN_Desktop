namespace Rojan.Desktop.Domain.Inventory;

/// <summary>
/// How a <see cref="StockTransaction"/> moves an <see cref="InventoryItem"/>'s
/// on-hand quantity - a genuine Domain rule (what "Received" vs. "Sold"
/// vs. "Adjustment" actually means to stock levels), not something
/// Application/Presentation should each reimplement.
/// </summary>
public static class StockTransactionRules
{
    /// <summary>Received/Returned/Sold/Damaged quantities are magnitudes and must be positive; an Adjustment is a signed correction delta and must be non-zero.</summary>
    public static bool IsValidQuantity(StockTransactionType type, int quantity) =>
        type == StockTransactionType.Adjustment ? quantity != 0 : quantity > 0;

    /// <summary>Applies the transaction to a current on-hand quantity, clamped at zero - physical stock can't go negative.</summary>
    public static int Apply(int currentQuantity, StockTransactionType type, int quantity)
    {
        var result = type switch
        {
            StockTransactionType.Received => currentQuantity + quantity,
            StockTransactionType.Returned => currentQuantity + quantity,
            StockTransactionType.Sold => currentQuantity - quantity,
            StockTransactionType.Damaged => currentQuantity - quantity,
            StockTransactionType.Adjustment => currentQuantity + quantity,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown stock transaction type."),
        };

        return Math.Max(0, result);
    }
}
