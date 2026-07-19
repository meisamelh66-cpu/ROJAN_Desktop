using DomainInventory = Rojan.Desktop.Domain.Inventory;

namespace Rojan.Desktop.Application.Inventory;

/// <summary>
/// Default <see cref="IProductProfileQueryService"/> implementation -
/// fetches the product plus its stock level, transaction history, and
/// service mappings from <see cref="DomainInventory.IInventoryRepository"/>
/// and assembles the aggregate <see cref="ProductProfileDto"/>.
/// </summary>
public sealed class ProductProfileQueryService : IProductProfileQueryService
{
    private readonly DomainInventory.IInventoryRepository _repository;

    public ProductProfileQueryService(DomainInventory.IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductProfileDto> GetProfileAsync(string productId, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetProductByIdAsync(productId, cancellationToken).ConfigureAwait(true);
        if (product is null)
        {
            throw new InvalidOperationException($"Product '{productId}' was not found.");
        }

        var stock = await _repository.GetInventoryItemByProductIdAsync(productId, cancellationToken).ConfigureAwait(true);
        var transactions = await _repository.GetTransactionsForProductAsync(productId, cancellationToken).ConfigureAwait(true);
        var mappings = await _repository.GetServiceMappingsForProductAsync(productId, cancellationToken).ConfigureAwait(true);

        return new ProductProfileDto(
            InventoryMapper.MapProduct(product),
            stock is null ? null : InventoryMapper.MapInventoryItem(stock),
            transactions.OrderByDescending(transaction => transaction.OccurredAt).Select(InventoryMapper.MapTransaction).ToList(),
            mappings.Select(InventoryMapper.MapServiceMapping).ToList());
    }
}
