namespace Rojan.Desktop.Application.Inventory;

/// <summary>Application-layer shape of a catalog product, mapped from <see cref="Rojan.Desktop.Domain.Inventory.Product"/> by <see cref="InventoryMapper"/>.</summary>
public sealed record ProductDto(
    string Id,
    string Sku,
    string Name,
    string CategoryId,
    string CategoryName,
    string SupplierId,
    string SupplierName,
    string UnitPrice,
    ProductStatus Status,
    string Description,
    string OrganizationId,
    string BranchId);
