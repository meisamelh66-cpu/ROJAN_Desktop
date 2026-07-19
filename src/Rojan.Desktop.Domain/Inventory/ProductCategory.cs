namespace Rojan.Desktop.Domain.Inventory;

/// <summary>A product catalog grouping, as returned by <see cref="IInventoryRepository"/>.</summary>
public sealed record ProductCategory(string Id, string Name, string Description);
