using Rojan.Desktop.Application.Inventory;

namespace Rojan.Desktop.Application.Tests.Accounting;

/// <summary>Records every stock transaction it receives so <see cref="InvoiceCommandServiceTests"/> can assert "Integrate with Inventory" without a real Inventory slice - only <see cref="RecordStockTransactionAsync"/> is exercised by <see cref="Rojan.Desktop.Application.Accounting.InvoiceCommandService"/>.</summary>
internal sealed class StubInventoryCommandService : IInventoryCommandService
{
    public List<(string ProductId, StockTransactionType Type, int Quantity, string Notes)> RecordTransactionCalls { get; } = [];

    public Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<ProductCategoryDto> CreateCategoryAsync(string name, string description, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<StockTransactionDto> RecordStockTransactionAsync(string productId, StockTransactionType type, int quantity, string notes, CancellationToken cancellationToken = default)
    {
        RecordTransactionCalls.Add((productId, type, quantity, notes));
        return Task.FromResult(new StockTransactionDto("new-transaction", productId, "Test Product", type, quantity, DateTimeOffset.UnixEpoch, notes));
    }

    public Task<ServiceProductMappingDto> MapProductToServiceAsync(string productId, string serviceName, int quantityPerService, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task UnmapProductFromServiceAsync(string productId, string mappingId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
