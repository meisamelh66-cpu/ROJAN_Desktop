namespace Rojan.Desktop.Application.Inventory;

/// <summary>Application-layer shape of a product category, mapped from <see cref="Rojan.Desktop.Domain.Inventory.ProductCategory"/> by <see cref="InventoryMapper"/>.</summary>
public sealed record ProductCategoryDto(string Id, string Name, string Description);
