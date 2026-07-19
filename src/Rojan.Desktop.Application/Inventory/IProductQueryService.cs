namespace Rojan.Desktop.Application.Inventory;

/// <summary>Read-only use case Presentation depends on to load Products - the only way Presentation ever reaches product data, never through Domain/Infrastructure directly.</summary>
public interface IProductQueryService
{
    public Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns products whose name, SKU, category, or description contains <paramref name="searchText"/> (case-insensitive); an empty/whitespace search returns every product.</summary>
    public Task<IReadOnlyList<ProductDto>> SearchProductsAsync(string searchText, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ProductCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default);
}
