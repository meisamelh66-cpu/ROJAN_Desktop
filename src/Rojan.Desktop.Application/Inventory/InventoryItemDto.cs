namespace Rojan.Desktop.Application.Inventory;

/// <summary>Application-layer shape of a product's stock level, mapped from <see cref="Rojan.Desktop.Domain.Inventory.InventoryItem"/> by <see cref="InventoryMapper"/>.</summary>
public sealed record InventoryItemDto(string Id, string ProductId, string ProductName, int QuantityOnHand, int ReorderThreshold)
{
    /// <summary>True when stock is at or below its reorder point - the low-stock monitoring signal every consumer (list badge, KPI count) reads from.</summary>
    public bool IsLowStock => QuantityOnHand <= ReorderThreshold;
}
