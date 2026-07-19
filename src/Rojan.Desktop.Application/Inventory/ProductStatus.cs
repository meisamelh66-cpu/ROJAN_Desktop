namespace Rojan.Desktop.Application.Inventory;

/// <summary>Application's own copy of <see cref="Rojan.Desktop.Domain.Inventory.ProductStatus"/> - distinct from Domain, same reasoning as <c>Customers.CustomerStatus</c>.</summary>
public enum ProductStatus
{
    Active,
    Discontinued,
    OutOfStock,
}
