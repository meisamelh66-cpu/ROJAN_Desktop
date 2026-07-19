using Rojan.Desktop.Application.Accounting;

namespace Rojan.Desktop.Presentation.Tests.Accounting;

/// <summary>Records every call it receives so ViewModel tests can assert a command was invoked with the right arguments - same reasoning as Inventory.StubInventoryCommandService.</summary>
internal sealed class StubInvoiceCommandService : IInvoiceCommandService
{
    private readonly Func<CreateInvoiceRequest, CancellationToken, Task<InvoiceDto>>? _createInvoice;
    private readonly Func<string, CancellationToken, Task<InvoiceDto>>? _cancelInvoice;

    public List<CreateInvoiceRequest> CreateRequests { get; } = [];

    public List<string> CancelledInvoiceIds { get; } = [];

    public StubInvoiceCommandService(
        Func<CreateInvoiceRequest, CancellationToken, Task<InvoiceDto>>? createInvoice = null,
        Func<string, CancellationToken, Task<InvoiceDto>>? cancelInvoice = null)
    {
        _createInvoice = createInvoice;
        _cancelInvoice = cancelInvoice;
    }

    public Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        CreateRequests.Add(request);
        return _createInvoice?.Invoke(request, cancellationToken) ?? Task.FromResult(new InvoiceDto(
            "invoice-new", request.CustomerId, request.CustomerName, request.BookingId, request.BookingReference,
            DateTimeOffset.Now, InvoiceStatus.Issued, 0m, 0m, 0m, request.Notes));
    }

    public Task<InvoiceDto> CancelInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        CancelledInvoiceIds.Add(invoiceId);
        return _cancelInvoice?.Invoke(invoiceId, cancellationToken) ?? Task.FromResult(new InvoiceDto(
            invoiceId, "customer-1", "Amelia Hart", string.Empty, string.Empty, DateTimeOffset.Now, InvoiceStatus.Cancelled, 0m, 0m, 0m, string.Empty));
    }
}
