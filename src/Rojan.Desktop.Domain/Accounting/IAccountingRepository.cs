namespace Rojan.Desktop.Domain.Accounting;

/// <summary>
/// Repository abstraction for the Accounting vertical slice - covers all
/// five related aggregate types (Invoice, InvoiceItem, Payment, Receipt,
/// CashSession) in one interface, same "one repository per slice"
/// convention every other module follows (see <c>Inventory.IInventoryRepository</c>
/// for the precedent covering an even wider slice). Domain defines the
/// contract; Infrastructure provides the concrete implementation (a
/// fake/in-memory one for now - Phase 18 explicitly has no backend
/// integration yet, same as every other vertical slice in this app).
/// Deliberately "dumb" - invoice totals and payment-driven status
/// (<see cref="InvoiceCalculator"/>, <see cref="InvoicePaymentRules"/>)
/// are Application's job, not this repository's.
/// </summary>
public interface IAccountingRepository
{
    public Task<IReadOnlyList<Invoice>> GetInvoicesAsync(CancellationToken cancellationToken = default);

    public Task<Invoice?> GetInvoiceByIdAsync(string invoiceId, CancellationToken cancellationToken = default);

    public Task<Invoice> CreateInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default);

    public Task<Invoice> UpdateInvoiceStatusAsync(string invoiceId, InvoiceStatus status, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<InvoiceItem>> GetInvoiceItemsAsync(string invoiceId, CancellationToken cancellationToken = default);

    public Task<InvoiceItem> AddInvoiceItemAsync(InvoiceItem item, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<Payment>> GetPaymentsAsync(CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<Payment>> GetPaymentsForInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default);

    public Task<Payment> RecordPaymentAsync(Payment payment, CancellationToken cancellationToken = default);

    public Task<Receipt> CreateReceiptAsync(Receipt receipt, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<Receipt>> GetReceiptsForInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<CashSession>> GetCashSessionsAsync(CancellationToken cancellationToken = default);

    public Task<CashSession?> GetOpenCashSessionAsync(CancellationToken cancellationToken = default);

    public Task<CashSession> OpenCashSessionAsync(CashSession session, CancellationToken cancellationToken = default);

    public Task<CashSession> CloseCashSessionAsync(string sessionId, decimal closingBalance, CancellationToken cancellationToken = default);
}
