using Rojan.Desktop.Application.Inventory;

namespace Rojan.Desktop.Application.Tests.Accounting;

/// <summary>Minimal <see cref="IProductQueryService"/> test double - only <see cref="GetProductsAsync"/> is exercised by <see cref="InvoiceQueryServiceTests"/>.</summary>
internal sealed class StubProductQueryService : IProductQueryService
{
    private readonly IReadOnlyList<ProductDto> _products;

    public StubProductQueryService(IReadOnlyList<ProductDto> products)
    {
        _products = products;
    }

    public Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_products);

    public Task<IReadOnlyList<ProductDto>> SearchProductsAsync(string searchText, CancellationToken cancellationToken = default) =>
        Task.FromResult(_products);

    public Task<IReadOnlyList<ProductCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProductCategoryDto>>([]);

    public Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SupplierDto>>([]);
}
