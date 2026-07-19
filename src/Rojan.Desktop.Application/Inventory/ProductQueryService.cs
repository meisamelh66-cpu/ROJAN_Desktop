using DomainInventory = Rojan.Desktop.Domain.Inventory;

namespace Rojan.Desktop.Application.Inventory;

/// <summary>
/// Default <see cref="IProductQueryService"/> implementation - fetches
/// from <see cref="DomainInventory.IInventoryRepository"/> (Application
/// is allowed to depend on Domain) and maps every Domain type to its
/// Application-owned equivalent via <see cref="InventoryMapper"/>, so
/// nothing Domain-shaped ever crosses into Presentation.
/// </summary>
public sealed class ProductQueryService : IProductQueryService
{
    private readonly DomainInventory.IInventoryRepository _repository;

    public ProductQueryService(DomainInventory.IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await _repository.GetProductsAsync(cancellationToken).ConfigureAwait(true);
        return products.Select(InventoryMapper.MapProduct).ToList();
    }

    /// <summary>
    /// Composes over <see cref="DomainInventory.IInventoryRepository.GetProductsAsync"/>
    /// rather than a dedicated repository search method - same reasoning
    /// as <c>Customers.CustomerQueryService.SearchCustomersAsync</c>.
    /// </summary>
    public async Task<IReadOnlyList<ProductDto>> SearchProductsAsync(string searchText, CancellationToken cancellationToken = default)
    {
        var products = await _repository.GetProductsAsync(cancellationToken).ConfigureAwait(true);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return products.Select(InventoryMapper.MapProduct).ToList();
        }

        return products
            .Where(product =>
                product.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                product.Sku.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                product.CategoryName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                product.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .Select(InventoryMapper.MapProduct)
            .ToList();
    }

    public async Task<IReadOnlyList<ProductCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _repository.GetCategoriesAsync(cancellationToken).ConfigureAwait(true);
        return categories.Select(InventoryMapper.MapCategory).ToList();
    }

    public async Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default)
    {
        var suppliers = await _repository.GetSuppliersAsync(cancellationToken).ConfigureAwait(true);
        return suppliers.Select(InventoryMapper.MapSupplier).ToList();
    }
}
