namespace Rojan.Desktop.Application.Inventory;

/// <summary>Application's own copy of <see cref="Rojan.Desktop.Domain.Inventory.SupplierStatus"/> - distinct from Domain, same reasoning as <c>Customers.CustomerStatus</c>.</summary>
public enum SupplierStatus
{
    Active,
    Inactive,
}
