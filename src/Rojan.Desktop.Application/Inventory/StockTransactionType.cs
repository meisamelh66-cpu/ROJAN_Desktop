namespace Rojan.Desktop.Application.Inventory;

/// <summary>Application's own copy of <see cref="Rojan.Desktop.Domain.Inventory.StockTransactionType"/> - distinct from Domain, same reasoning as <c>Customers.CustomerStatus</c>.</summary>
public enum StockTransactionType
{
    Received,
    Sold,
    Returned,
    Damaged,
    Adjustment,
}
