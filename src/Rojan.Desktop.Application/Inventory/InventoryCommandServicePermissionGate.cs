using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Inventory;

/// <summary>Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Every method requires <see cref="Permission.InventoryEdit"/>.</summary>
public sealed class InventoryCommandServicePermissionGate : IInventoryCommandService
{
    private readonly IInventoryCommandService _inner;
    private readonly IPermissionGate _permissionGate;

    public InventoryCommandServicePermissionGate(IInventoryCommandService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.InventoryEdit);
        return _inner.CreateProductAsync(request, cancellationToken);
    }

    public Task<ProductCategoryDto> CreateCategoryAsync(string name, string description, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.InventoryEdit);
        return _inner.CreateCategoryAsync(name, description, cancellationToken);
    }

    public Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.InventoryEdit);
        return _inner.CreateSupplierAsync(request, cancellationToken);
    }

    public Task<StockTransactionDto> RecordStockTransactionAsync(string productId, StockTransactionType type, int quantity, string notes, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.InventoryEdit);
        return _inner.RecordStockTransactionAsync(productId, type, quantity, notes, cancellationToken);
    }

    public Task<ServiceProductMappingDto> MapProductToServiceAsync(string productId, string serviceName, int quantityPerService, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.InventoryEdit);
        return _inner.MapProductToServiceAsync(productId, serviceName, quantityPerService, cancellationToken);
    }

    public Task UnmapProductFromServiceAsync(string productId, string mappingId, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.InventoryEdit);
        return _inner.UnmapProductFromServiceAsync(productId, mappingId, cancellationToken);
    }
}
