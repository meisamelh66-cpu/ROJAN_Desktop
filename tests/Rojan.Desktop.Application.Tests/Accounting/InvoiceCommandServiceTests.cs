using Rojan.Desktop.Application.Accounting;
using AppInventory = Rojan.Desktop.Application.Inventory;
using DomainAccounting = Rojan.Desktop.Domain.Accounting;

namespace Rojan.Desktop.Application.Tests.Accounting;

public sealed class InvoiceCommandServiceTests
{
    private static InvoiceCommandService MakeSut(StubAccountingRepository? repository = null, StubInventoryCommandService? inventoryCommandService = null) =>
        new(repository ?? new StubAccountingRepository(), inventoryCommandService ?? new StubInventoryCommandService());

    [Fact]
    public async Task CreateInvoiceAsync_ValidRequest_ComputesTotalsAndWritesInvoiceAndItems()
    {
        var repository = new StubAccountingRepository();
        var sut = MakeSut(repository);
        var request = new CreateInvoiceRequest(
            "customer-1", "Amelia Hart", "booking-1", "Manicure - Amelia Hart",
            [
                new CreateInvoiceItemRequest(string.Empty, "service-4", "Manicure", 1, 40m),
                new CreateInvoiceItemRequest("product-6", string.Empty, "Gel Polish", 1, 9m),
            ],
            0.08m, string.Empty);

        var created = await sut.CreateInvoiceAsync(request);

        Assert.Equal(49m, created.Subtotal);
        Assert.Equal(3.92m, created.TaxAmount);
        Assert.Equal(52.92m, created.Total);
        Assert.Equal(InvoiceStatus.Issued, created.Status);
        Assert.Single(repository.Invoices);
        Assert.Equal(2, repository.Items.Count);
    }

    [Fact]
    public async Task CreateInvoiceAsync_NoItems_ThrowsArgumentExceptionAndWritesNothing()
    {
        var repository = new StubAccountingRepository();
        var sut = MakeSut(repository);
        var request = new CreateInvoiceRequest("customer-1", "Amelia Hart", string.Empty, string.Empty, [], 0.08m, string.Empty);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateInvoiceAsync(request));

        Assert.Empty(repository.Invoices);
    }

    [Fact]
    public async Task CreateInvoiceAsync_LineItemWithProductId_DecrementsStockViaInventoryCommandService()
    {
        var inventoryCommandService = new StubInventoryCommandService();
        var sut = MakeSut(inventoryCommandService: inventoryCommandService);
        var request = new CreateInvoiceRequest(
            "customer-1", "Amelia Hart", string.Empty, string.Empty,
            [new CreateInvoiceItemRequest("product-6", string.Empty, "Gel Polish", 2, 9m)],
            0.08m, string.Empty);

        await sut.CreateInvoiceAsync(request);

        var call = Assert.Single(inventoryCommandService.RecordTransactionCalls);
        Assert.Equal("product-6", call.ProductId);
        Assert.Equal(AppInventory.StockTransactionType.Sold, call.Type);
        Assert.Equal(2, call.Quantity);
    }

    [Fact]
    public async Task CreateInvoiceAsync_LineItemWithoutProductId_DoesNotCallInventoryCommandService()
    {
        var inventoryCommandService = new StubInventoryCommandService();
        var sut = MakeSut(inventoryCommandService: inventoryCommandService);
        var request = new CreateInvoiceRequest(
            "customer-1", "Amelia Hart", string.Empty, string.Empty,
            [new CreateInvoiceItemRequest(string.Empty, "service-4", "Manicure", 1, 40m)],
            0.08m, string.Empty);

        await sut.CreateInvoiceAsync(request);

        Assert.Empty(inventoryCommandService.RecordTransactionCalls);
    }

    [Fact]
    public async Task CancelInvoiceAsync_ExistingInvoice_SetsStatusToCancelled()
    {
        var repository = new StubAccountingRepository();
        repository.Invoices.Add(new DomainAccounting.Invoice("invoice-1", "customer-1", "Amelia Hart", string.Empty, string.Empty, DateTimeOffset.UnixEpoch, DomainAccounting.InvoiceStatus.Issued, 40m, 3.20m, 43.20m, string.Empty));
        var sut = MakeSut(repository);

        var result = await sut.CancelInvoiceAsync("invoice-1");

        Assert.Equal(InvoiceStatus.Cancelled, result.Status);
    }
}
