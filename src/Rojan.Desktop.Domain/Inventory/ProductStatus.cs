namespace Rojan.Desktop.Domain.Inventory;

/// <summary>Catalog availability of a product, as returned by <see cref="IInventoryRepository"/>.</summary>
public enum ProductStatus
{
    Active,
    Discontinued,
    OutOfStock,
}
