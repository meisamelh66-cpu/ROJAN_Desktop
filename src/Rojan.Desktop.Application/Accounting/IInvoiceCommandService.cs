namespace Rojan.Desktop.Application.Accounting;

/// <summary>Write use cases for Invoices - creation (the POS checkout's cart-to-invoice step) and cancellation.</summary>
public interface IInvoiceCommandService
{
    public Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default);

    public Task<InvoiceDto> CancelInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default);
}
