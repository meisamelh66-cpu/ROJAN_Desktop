using Rojan.Desktop.Application.Inventory;

namespace Rojan.Desktop.Presentation.Tests.Inventory;

/// <summary>Configurable <see cref="IProductQueryService"/> test double - same reasoning as Customers.StubCustomerQueryService.</summary>
internal sealed class StubProductQueryService : IProductQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<ProductDto>>> _getProducts;
    private readonly Func<string, CancellationToken, Task<IReadOnlyList<ProductDto>>>? _searchProducts;
    private readonly Func<CancellationToken, Task<IReadOnlyList<ProductCategoryDto>>>? _getCategories;
    private readonly Func<CancellationToken, Task<IReadOnlyList<SupplierDto>>>? _getSuppliers;

    public StubProductQueryService(
        Func<CancellationToken, Task<IReadOnlyList<ProductDto>>> getProducts,
        Func<string, CancellationToken, Task<IReadOnlyList<ProductDto>>>? searchProducts = null,
        Func<CancellationToken, Task<IReadOnlyList<ProductCategoryDto>>>? getCategories = null,
        Func<CancellationToken, Task<IReadOnlyList<SupplierDto>>>? getSuppliers = null)
    {
        _getProducts = getProducts;
        _searchProducts = searchProducts;
        _getCategories = getCategories;
        _getSuppliers = getSuppliers;
    }

    public Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default) =>
        _getProducts(cancellationToken);

    public async Task<IReadOnlyList<ProductDto>> SearchProductsAsync(string searchText, CancellationToken cancellationToken = default)
    {
        if (_searchProducts is not null)
        {
            return await _searchProducts(searchText, cancellationToken).ConfigureAwait(true);
        }

        var products = await _getProducts(cancellationToken).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return products;
        }

        return products
            .Where(product =>
                product.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                product.Sku.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public Task<IReadOnlyList<ProductCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        _getCategories?.Invoke(cancellationToken) ?? Task.FromResult<IReadOnlyList<ProductCategoryDto>>([]);

    public Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default) =>
        _getSuppliers?.Invoke(cancellationToken) ?? Task.FromResult<IReadOnlyList<SupplierDto>>([]);
}
