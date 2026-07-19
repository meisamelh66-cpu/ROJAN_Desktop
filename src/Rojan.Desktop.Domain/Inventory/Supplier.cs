namespace Rojan.Desktop.Domain.Inventory;

/// <summary>A single supplier record, as returned by <see cref="IInventoryRepository"/>.</summary>
public sealed record Supplier(string Id, string Name, string ContactName, string Email, string Phone, SupplierStatus Status);
