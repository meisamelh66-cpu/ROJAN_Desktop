namespace Rojan.Desktop.Application.Inventory;

/// <summary>Application-layer shape of a supplier, mapped from <see cref="Rojan.Desktop.Domain.Inventory.Supplier"/> by <see cref="InventoryMapper"/>.</summary>
public sealed record SupplierDto(string Id, string Name, string ContactName, string Email, string Phone, SupplierStatus Status);
