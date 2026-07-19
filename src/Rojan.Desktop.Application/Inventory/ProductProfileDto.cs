namespace Rojan.Desktop.Application.Inventory;

/// <summary>Everything the Product profile screen needs for one product - stock level, recent transactions, and linked services, fetched together as a single unit of work, same reasoning as Customers.CustomerProfileDto/Services.ServiceProfileDto.</summary>
public sealed record ProductProfileDto(
    ProductDto Product,
    InventoryItemDto? Stock,
    IReadOnlyList<StockTransactionDto> RecentTransactions,
    IReadOnlyList<ServiceProductMappingDto> ServiceMappings);
