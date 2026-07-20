using Rojan.Desktop.Application.Inventory;

namespace Rojan.Desktop.Presentation.Tests.Inventory;

/// <summary>Records every call it receives so ViewModel tests can assert a command was invoked with the right arguments - same reasoning as Customers.StubCustomerCommandService.</summary>
internal sealed class StubInventoryCommandService : IInventoryCommandService
{
    public List<CreateProductRequest> CreateProductRequests { get; } = [];

    public List<(string Name, string Description)> CreateCategoryCalls { get; } = [];

    public List<CreateSupplierRequest> CreateSupplierRequests { get; } = [];

    public List<(string ProductId, StockTransactionType Type, int Quantity, string Notes)> RecordTransactionCalls { get; } = [];

    public List<(string ProductId, string ServiceName, int QuantityPerService)> MapServiceCalls { get; } = [];

    public List<(string ProductId, string MappingId)> UnmapServiceCalls { get; } = [];

    public Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        CreateProductRequests.Add(request);
        return Task.FromResult(new ProductDto(
            "new-product", request.Sku, request.Name, request.CategoryId, request.CategoryName,
            request.SupplierId, request.SupplierName, request.UnitPrice, ProductStatus.Active, request.Description, "org-1", "branch-1"));
    }

    public Task<ProductCategoryDto> CreateCategoryAsync(string name, string description, CancellationToken cancellationToken = default)
    {
        CreateCategoryCalls.Add((name, description));
        return Task.FromResult(new ProductCategoryDto("new-category", name, description));
    }

    public Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        CreateSupplierRequests.Add(request);
        return Task.FromResult(new SupplierDto("new-supplier", request.Name, request.ContactName, request.Email, request.Phone, SupplierStatus.Active));
    }

    public Task<StockTransactionDto> RecordStockTransactionAsync(string productId, StockTransactionType type, int quantity, string notes, CancellationToken cancellationToken = default)
    {
        RecordTransactionCalls.Add((productId, type, quantity, notes));
        return Task.FromResult(new StockTransactionDto("new-transaction", productId, "Test Product", type, quantity, DateTimeOffset.UnixEpoch, notes));
    }

    public Task<ServiceProductMappingDto> MapProductToServiceAsync(string productId, string serviceName, int quantityPerService, CancellationToken cancellationToken = default)
    {
        MapServiceCalls.Add((productId, serviceName, quantityPerService));
        return Task.FromResult(new ServiceProductMappingDto("new-mapping", "new-service", serviceName, productId, "Test Product", quantityPerService));
    }

    public Task UnmapProductFromServiceAsync(string productId, string mappingId, CancellationToken cancellationToken = default)
    {
        UnmapServiceCalls.Add((productId, mappingId));
        return Task.CompletedTask;
    }
}
