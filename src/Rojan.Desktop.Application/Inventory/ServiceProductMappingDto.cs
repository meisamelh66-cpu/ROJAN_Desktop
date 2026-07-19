namespace Rojan.Desktop.Application.Inventory;

/// <summary>Application-layer shape of a service-to-product mapping, mapped from <see cref="Rojan.Desktop.Domain.Inventory.ServiceProductMapping"/> by <see cref="InventoryMapper"/>.</summary>
public sealed record ServiceProductMappingDto(string Id, string ServiceId, string ServiceName, string ProductId, string ProductName, int QuantityPerService);
