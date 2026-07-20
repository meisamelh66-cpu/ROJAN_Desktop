using Rojan.Desktop.Application.Organizations;
using DomainInventory = Rojan.Desktop.Domain.Inventory;

namespace Rojan.Desktop.Application.Inventory;

/// <summary>Default <see cref="IInventoryCommandService"/> implementation. Phase 22A: <see cref="CreateProductAsync"/> stamps the new product with the current session's organization/branch (<see cref="IEnterpriseContext"/>).</summary>
public sealed class InventoryCommandService : IInventoryCommandService
{
    private readonly DomainInventory.IInventoryRepository _repository;
    private readonly IEnterpriseContext _enterpriseContext;

    public InventoryCommandService(DomainInventory.IInventoryRepository repository, IEnterpriseContext enterpriseContext)
    {
        _repository = repository;
        _enterpriseContext = enterpriseContext;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new DomainInventory.Product(
            Guid.NewGuid().ToString(),
            request.Sku,
            request.Name,
            request.CategoryId,
            request.CategoryName,
            request.SupplierId,
            request.SupplierName,
            request.UnitPrice,
            DomainInventory.ProductStatus.Active,
            request.Description,
            _enterpriseContext.CurrentOrganizationId ?? string.Empty,
            _enterpriseContext.CurrentBranchId ?? string.Empty);

        var created = await _repository.CreateProductAsync(product, cancellationToken).ConfigureAwait(true);

        var inventoryItem = new DomainInventory.InventoryItem(
            Guid.NewGuid().ToString(),
            created.Id,
            created.Name,
            request.InitialQuantity,
            request.ReorderThreshold);
        await _repository.CreateInventoryItemAsync(inventoryItem, cancellationToken).ConfigureAwait(true);

        return InventoryMapper.MapProduct(created);
    }

    public async Task<ProductCategoryDto> CreateCategoryAsync(string name, string description, CancellationToken cancellationToken = default)
    {
        var category = new DomainInventory.ProductCategory(Guid.NewGuid().ToString(), name, description);
        var created = await _repository.CreateCategoryAsync(category, cancellationToken).ConfigureAwait(true);
        return InventoryMapper.MapCategory(created);
    }

    public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = new DomainInventory.Supplier(
            Guid.NewGuid().ToString(),
            request.Name,
            request.ContactName,
            request.Email,
            request.Phone,
            DomainInventory.SupplierStatus.Active);

        var created = await _repository.CreateSupplierAsync(supplier, cancellationToken).ConfigureAwait(true);
        return InventoryMapper.MapSupplier(created);
    }

    public async Task<StockTransactionDto> RecordStockTransactionAsync(string productId, StockTransactionType type, int quantity, string notes, CancellationToken cancellationToken = default)
    {
        var domainType = InventoryMapper.MapTransactionTypeToDomain(type);
        if (!DomainInventory.StockTransactionRules.IsValidQuantity(domainType, quantity))
        {
            throw new ArgumentException($"Quantity {quantity} is not valid for transaction type {type}.", nameof(quantity));
        }

        var stock = await _repository.GetInventoryItemByProductIdAsync(productId, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Product '{productId}' has no inventory record.");

        var newQuantity = DomainInventory.StockTransactionRules.Apply(stock.QuantityOnHand, domainType, quantity);
        await _repository.UpdateInventoryQuantityAsync(productId, newQuantity, cancellationToken).ConfigureAwait(true);

        var transaction = new DomainInventory.StockTransaction(
            Guid.NewGuid().ToString(),
            productId,
            stock.ProductName,
            domainType,
            quantity,
            DateTimeOffset.Now,
            notes);
        var recorded = await _repository.RecordTransactionAsync(transaction, cancellationToken).ConfigureAwait(true);

        return InventoryMapper.MapTransaction(recorded);
    }

    public async Task<ServiceProductMappingDto> MapProductToServiceAsync(string productId, string serviceName, int quantityPerService, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetProductByIdAsync(productId, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Product '{productId}' was not found.");

        var mapping = new DomainInventory.ServiceProductMapping(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            serviceName,
            productId,
            product.Name,
            quantityPerService);

        var created = await _repository.MapProductToServiceAsync(mapping, cancellationToken).ConfigureAwait(true);
        return InventoryMapper.MapServiceMapping(created);
    }

    public Task UnmapProductFromServiceAsync(string productId, string mappingId, CancellationToken cancellationToken = default) =>
        _repository.UnmapProductFromServiceAsync(productId, mappingId, cancellationToken);
}
