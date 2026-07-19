namespace Rojan.Desktop.Application.Inventory;

/// <summary>Application-layer shape of a stock movement, mapped from <see cref="Rojan.Desktop.Domain.Inventory.StockTransaction"/> by <see cref="InventoryMapper"/>.</summary>
public sealed record StockTransactionDto(
    string Id,
    string ProductId,
    string ProductName,
    StockTransactionType Type,
    int Quantity,
    DateTimeOffset OccurredAt,
    string Notes);
