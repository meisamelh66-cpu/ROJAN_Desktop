using Rojan.Desktop.Domain.Accounting;

namespace Rojan.Desktop.Application.Tests.Accounting;

/// <summary>In-memory, mutable <see cref="IAccountingRepository"/> test double - same reasoning as Inventory.StubInventoryRepository, covering all five Accounting aggregate types.</summary>
internal sealed class StubAccountingRepository : IAccountingRepository
{
    public List<Invoice> Invoices { get; } = [];

    public List<InvoiceItem> Items { get; } = [];

    public List<Payment> Payments { get; } = [];

    public List<Receipt> Receipts { get; } = [];

    public List<CashSession> CashSessions { get; } = [];

    public Task<IReadOnlyList<Invoice>> GetInvoicesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Invoice>>(Invoices.ToList());

    public Task<Invoice?> GetInvoiceByIdAsync(string invoiceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invoices.FirstOrDefault(invoice => invoice.Id == invoiceId));

    public Task<Invoice> CreateInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        Invoices.Add(invoice);
        return Task.FromResult(invoice);
    }

    public Task<Invoice> UpdateInvoiceStatusAsync(string invoiceId, InvoiceStatus status, CancellationToken cancellationToken = default)
    {
        var index = Invoices.FindIndex(invoice => invoice.Id == invoiceId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Invoice '{invoiceId}' was not found.");
        }

        var updated = Invoices[index] with { Status = status };
        Invoices[index] = updated;
        return Task.FromResult(updated);
    }

    public Task<IReadOnlyList<InvoiceItem>> GetInvoiceItemsAsync(string invoiceId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<InvoiceItem>>(Items.Where(item => item.InvoiceId == invoiceId).ToList());

    public Task<InvoiceItem> AddInvoiceItemAsync(InvoiceItem item, CancellationToken cancellationToken = default)
    {
        Items.Add(item);
        return Task.FromResult(item);
    }

    public Task<IReadOnlyList<Payment>> GetPaymentsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Payment>>(Payments.ToList());

    public Task<IReadOnlyList<Payment>> GetPaymentsForInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Payment>>(Payments.Where(payment => payment.InvoiceId == invoiceId).ToList());

    public Task<Payment> RecordPaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        Payments.Add(payment);
        return Task.FromResult(payment);
    }

    public Task<Receipt> CreateReceiptAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        Receipts.Add(receipt);
        return Task.FromResult(receipt);
    }

    public Task<IReadOnlyList<Receipt>> GetReceiptsForInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Receipt>>(Receipts.Where(receipt => receipt.InvoiceId == invoiceId).ToList());

    public Task<IReadOnlyList<CashSession>> GetCashSessionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CashSession>>(CashSessions.ToList());

    public Task<CashSession?> GetOpenCashSessionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CashSessions.FirstOrDefault(session => session.Status == CashSessionStatus.Open));

    public Task<CashSession> OpenCashSessionAsync(CashSession session, CancellationToken cancellationToken = default)
    {
        CashSessions.Add(session);
        return Task.FromResult(session);
    }

    public Task<CashSession> CloseCashSessionAsync(string sessionId, decimal closingBalance, CancellationToken cancellationToken = default)
    {
        var index = CashSessions.FindIndex(session => session.Id == sessionId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Cash session '{sessionId}' was not found.");
        }

        var updated = CashSessions[index] with { ClosedAt = DateTimeOffset.Now, ClosingBalance = closingBalance, Status = CashSessionStatus.Closed };
        CashSessions[index] = updated;
        return Task.FromResult(updated);
    }
}
