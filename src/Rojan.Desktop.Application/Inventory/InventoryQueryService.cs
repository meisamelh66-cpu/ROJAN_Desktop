using DomainInventory = Rojan.Desktop.Domain.Inventory;

namespace Rojan.Desktop.Application.Inventory;

/// <summary>Default <see cref="IInventoryQueryService"/> implementation.</summary>
public sealed class InventoryQueryService : IInventoryQueryService
{
    private readonly DomainInventory.IInventoryRepository _repository;

    public InventoryQueryService(DomainInventory.IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetInventoryItemsAsync(cancellationToken).ConfigureAwait(true);
        return items.Select(InventoryMapper.MapInventoryItem).ToList();
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetLowStockItemsAsync(CancellationToken cancellationToken = default)
    {
        var items = await GetInventoryItemsAsync(cancellationToken).ConfigureAwait(true);
        return items.Where(item => item.IsLowStock).ToList();
    }
}
