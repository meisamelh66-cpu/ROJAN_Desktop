using Rojan.Desktop.Application.Organizations;
using DomainInventory = Rojan.Desktop.Domain.Inventory;

namespace Rojan.Desktop.Application.Inventory;

/// <summary>
/// Default <see cref="IProductQueryService"/> implementation - fetches
/// from <see cref="DomainInventory.IInventoryRepository"/> (Application
/// is allowed to depend on Domain) and maps every Domain type to its
/// Application-owned equivalent via <see cref="InventoryMapper"/>, so
/// nothing Domain-shaped ever crosses into Presentation.
///
/// Phase 22A: products are scoped to <see cref="IEnterpriseContext"/> -
/// the "Products"/"Inventory" module's Organization/Branch Scoping
/// guarantee, same reasoning as <c>Customers.CustomerQueryService</c>.
/// Categories/Suppliers stay unscoped - they are shared catalog metadata
/// (e.g. "Skincare" as a category, "Acme Supply Co." as a supplier),
/// never a customer-owned or branch-owned record.
/// </summary>
public sealed class ProductQueryService : IProductQueryService
{
    private readonly DomainInventory.IInventoryRepository _repository;
    private readonly IEnterpriseContext _enterpriseContext;

    public ProductQueryService(DomainInventory.IInventoryRepository repository, IEnterpriseContext enterpriseContext)
    {
        _repository = repository;
        _enterpriseContext = enterpriseContext;
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await _repository.GetProductsAsync(cancellationToken).ConfigureAwait(true);
        return ScopeToCurrentSession(products).Select(InventoryMapper.MapProduct).ToList();
    }

    /// <summary>
    /// Composes over <see cref="DomainInventory.IInventoryRepository.GetProductsAsync"/>
    /// rather than a dedicated repository search method - same reasoning
    /// as <c>Customers.CustomerQueryService.SearchCustomersAsync</c>.
    /// </summary>
    public async Task<IReadOnlyList<ProductDto>> SearchProductsAsync(string searchText, CancellationToken cancellationToken = default)
    {
        var products = ScopeToCurrentSession(await _repository.GetProductsAsync(cancellationToken).ConfigureAwait(true));

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

    private IEnumerable<DomainInventory.Product> ScopeToCurrentSession(IEnumerable<DomainInventory.Product> products) =>
        products.Where(product =>
            product.OrganizationId == _enterpriseContext.CurrentOrganizationId &&
            (_enterpriseContext.CurrentBranchId is null || product.BranchId == _enterpriseContext.CurrentBranchId));

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
