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

    /// <summary>Production Hardening (missing-guard sweep, Wave C): when set, the matching command throws this instead of succeeding - lets a test exercise the ViewModel's new try/catch without a real backend failure. Same seam pattern as Customers.StubCustomerCommandService.CreateCustomerException. The call is still recorded before the throw.</summary>
    public Exception? CreateProductException { get; set; }

    public Exception? CreateCategoryException { get; set; }

    public Exception? CreateSupplierException { get; set; }

    public Exception? RecordStockTransactionException { get; set; }

    public Exception? MapProductToServiceException { get; set; }

    public Exception? UnmapProductFromServiceException { get; set; }

    public Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        CreateProductRequests.Add(request);
        if (CreateProductException is not null)
        {
            return Task.FromException<ProductDto>(CreateProductException);
        }

        return Task.FromResult(new ProductDto(
            "new-product", request.Sku, request.Name, request.CategoryId, request.CategoryName,
            request.SupplierId, request.SupplierName, request.UnitPrice, ProductStatus.Active, request.Description, "org-1", "branch-1"));
    }

    public Task<ProductCategoryDto> CreateCategoryAsync(string name, string description, CancellationToken cancellationToken = default)
    {
        CreateCategoryCalls.Add((name, description));
        return CreateCategoryException is not null
            ? Task.FromException<ProductCategoryDto>(CreateCategoryException)
            : Task.FromResult(new ProductCategoryDto("new-category", name, description));
    }

    public Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        CreateSupplierRequests.Add(request);
        return CreateSupplierException is not null
            ? Task.FromException<SupplierDto>(CreateSupplierException)
            : Task.FromResult(new SupplierDto("new-supplier", request.Name, request.ContactName, request.Email, request.Phone, SupplierStatus.Active));
    }

    public Task<StockTransactionDto> RecordStockTransactionAsync(string productId, StockTransactionType type, int quantity, string notes, CancellationToken cancellationToken = default)
    {
        RecordTransactionCalls.Add((productId, type, quantity, notes));
        return RecordStockTransactionException is not null
            ? Task.FromException<StockTransactionDto>(RecordStockTransactionException)
            : Task.FromResult(new StockTransactionDto("new-transaction", productId, "Test Product", type, quantity, DateTimeOffset.UnixEpoch, notes));
    }

    public Task<ServiceProductMappingDto> MapProductToServiceAsync(string productId, string serviceName, int quantityPerService, CancellationToken cancellationToken = default)
    {
        MapServiceCalls.Add((productId, serviceName, quantityPerService));
        return MapProductToServiceException is not null
            ? Task.FromException<ServiceProductMappingDto>(MapProductToServiceException)
            : Task.FromResult(new ServiceProductMappingDto("new-mapping", "new-service", serviceName, productId, "Test Product", quantityPerService));
    }

    public Task UnmapProductFromServiceAsync(string productId, string mappingId, CancellationToken cancellationToken = default)
    {
        UnmapServiceCalls.Add((productId, mappingId));
        return UnmapProductFromServiceException is not null ? Task.FromException(UnmapProductFromServiceException) : Task.CompletedTask;
    }
}
