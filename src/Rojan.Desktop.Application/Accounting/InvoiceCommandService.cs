using AppInventory = Rojan.Desktop.Application.Inventory;
using DomainAccounting = Rojan.Desktop.Domain.Accounting;

namespace Rojan.Desktop.Application.Accounting;

/// <summary>
/// Default <see cref="IInvoiceCommandService"/> implementation. Depends
/// on <see cref="AppInventory.IInventoryCommandService"/> to decrement
/// stock for every product line item ("Integrate with Inventory") - the
/// same cross-slice composition reasoning as
/// <c>BookingWorkflow.BookingWorkflowService</c> depending on
/// Calendar/Customer/Service/Specialist's Application services; Inventory's
/// own command service is used exactly as published, never modified.
/// </summary>
public sealed class InvoiceCommandService : IInvoiceCommandService
{
    private readonly DomainAccounting.IAccountingRepository _repository;
    private readonly AppInventory.IInventoryCommandService _inventoryCommandService;

    public InvoiceCommandService(DomainAccounting.IAccountingRepository repository, AppInventory.IInventoryCommandService inventoryCommandService)
    {
        _repository = repository;
        _inventoryCommandService = inventoryCommandService;
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new ArgumentException("An invoice needs at least one line item.", nameof(request));
        }

        var domainItems = request.Items
            .Select(item => (Request: item, LineTotal: DomainAccounting.InvoiceCalculator.ComputeLineTotal(item.Quantity, item.UnitPrice)))
            .ToList();

        var subtotal = domainItems.Sum(item => item.LineTotal);
        var taxAmount = DomainAccounting.InvoiceCalculator.ComputeTax(subtotal, request.TaxRate);
        var total = DomainAccounting.InvoiceCalculator.ComputeTotal(subtotal, taxAmount);

        var invoice = new DomainAccounting.Invoice(
            Guid.NewGuid().ToString(),
            request.CustomerId,
            request.CustomerName,
            request.BookingId,
            request.BookingReference,
            DateTimeOffset.Now,
            DomainAccounting.InvoiceStatus.Issued,
            subtotal,
            taxAmount,
            total,
            request.Notes);

        var created = await _repository.CreateInvoiceAsync(invoice, cancellationToken).ConfigureAwait(true);

        foreach (var (itemRequest, lineTotal) in domainItems)
        {
            var domainItem = new DomainAccounting.InvoiceItem(
                Guid.NewGuid().ToString(),
                created.Id,
                itemRequest.ProductId,
                itemRequest.ServiceId,
                itemRequest.Description,
                itemRequest.Quantity,
                itemRequest.UnitPrice,
                lineTotal);
            await _repository.AddInvoiceItemAsync(domainItem, cancellationToken).ConfigureAwait(true);

            if (!string.IsNullOrEmpty(itemRequest.ProductId))
            {
                await _inventoryCommandService.RecordStockTransactionAsync(
                    itemRequest.ProductId,
                    AppInventory.StockTransactionType.Sold,
                    itemRequest.Quantity,
                    $"Sold via invoice {created.Id}.",
                    cancellationToken).ConfigureAwait(true);
            }
        }

        return AccountingMapper.MapInvoice(created);
    }

    public async Task<InvoiceDto> CancelInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        var updated = await _repository.UpdateInvoiceStatusAsync(invoiceId, DomainAccounting.InvoiceStatus.Cancelled, cancellationToken).ConfigureAwait(true);
        return AccountingMapper.MapInvoice(updated);
    }
}
