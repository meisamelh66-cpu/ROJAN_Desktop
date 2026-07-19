using Rojan.Desktop.Application.Inventory;

namespace Rojan.Desktop.Presentation.Tests.Inventory;

/// <summary>Configurable <see cref="IInventoryQueryService"/> test double - same reasoning as Customers.StubCustomerQueryService.</summary>
internal sealed class StubInventoryQueryService : IInventoryQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<InventoryItemDto>>> _getItems;

    public StubInventoryQueryService(Func<CancellationToken, Task<IReadOnlyList<InventoryItemDto>>>? getItems = null)
    {
        _getItems = getItems ?? (_ => Task.FromResult<IReadOnlyList<InventoryItemDto>>([]));
    }

    public Task<IReadOnlyList<InventoryItemDto>> GetInventoryItemsAsync(CancellationToken cancellationToken = default) =>
        _getItems(cancellationToken);

    public async Task<IReadOnlyList<InventoryItemDto>> GetLowStockItemsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _getItems(cancellationToken).ConfigureAwait(true);
        return items.Where(item => item.IsLowStock).ToList();
    }
}
