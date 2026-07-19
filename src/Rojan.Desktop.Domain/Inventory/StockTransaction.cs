namespace Rojan.Desktop.Domain.Inventory;

/// <summary>
/// A single stock movement record, as returned by <see cref="IInventoryRepository"/>.
/// <see cref="Quantity"/> is the transaction's magnitude for every
/// <see cref="StockTransactionType"/> except <see cref="StockTransactionType.Adjustment"/>,
/// where it is a signed correction delta (see <see cref="StockTransactionRules"/>
/// for how each type actually moves an <see cref="InventoryItem"/>'s
/// on-hand quantity).
/// </summary>
public sealed record StockTransaction(
    string Id,
    string ProductId,
    string ProductName,
    StockTransactionType Type,
    int Quantity,
    DateTimeOffset OccurredAt,
    string Notes);
