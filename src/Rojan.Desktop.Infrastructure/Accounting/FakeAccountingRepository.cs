using Rojan.Desktop.Domain.Accounting;

namespace Rojan.Desktop.Infrastructure.Accounting;

/// <summary>
/// In-memory <see cref="IAccountingRepository"/> providing static sample
/// data - Phase 18 explicitly has no backend integration yet, same as
/// every other vertical slice in this app. Instance (not static) mutable
/// state, same reasoning as <c>Customers.FakeCustomerRepository</c>: this
/// fake has real create/payment/session commands, so it needs to remember
/// writes for the app's lifetime - registered as a DI singleton (see
/// Infrastructure's ServiceCollectionExtensions). Seed data cross-references
/// the real customer ids ("customer-1".."customer-7"), booking ids
/// ("booking-1".."booking-8"), service ids ("service-1".."service-9"), and
/// product ids ("product-1".."product-10") already seeded in
/// <c>Customers.FakeCustomerRepository</c>, <c>Bookings.FakeBookingRepository</c>,
/// <c>Services.FakeServiceRepository</c>, and <c>Inventory.FakeInventoryRepository</c>
/// for a cohesive demo - not a real cross-slice link, just consistent
/// naming, same reasoning as every other cross-slice reference in this
/// app. The small artificial delays are deliberate, same reasoning as
/// every other fake repository: without them, Loading states would never
/// actually be observable when running the app.
/// </summary>
public sealed class FakeAccountingRepository : IAccountingRepository
{
    private readonly List<Invoice> _invoices;
    private readonly List<InvoiceItem> _items;
    private readonly List<Payment> _payments;
    private readonly List<Receipt> _receipts;
    private readonly List<CashSession> _cashSessions;

    public FakeAccountingRepository()
    {
        var now = DateTimeOffset.Now;

        _invoices =
        [
            new Invoice("invoice-1", "customer-1", "Amelia Hart", "booking-5", "Manicure - Amelia Hart",
                now.AddDays(-5), InvoiceStatus.Paid, 64m, 5.12m, 69.12m, string.Empty),
            new Invoice("invoice-2", "customer-3", "Sophia Reyes", "booking-6", "Facial Renewal - Sophia Reyes",
                now.AddDays(-10), InvoiceStatus.Paid, 107m, 8.56m, 115.56m, string.Empty),
            new Invoice("invoice-3", "customer-4", "Liam Foster", "booking-7", "Haircut & Style - Liam Foster",
                now.AddDays(-95), InvoiceStatus.Paid, 83m, 6.64m, 89.64m, string.Empty),
            new Invoice("invoice-4", "customer-6", "Ethan Brooks", "booking-8", "Haircut & Style - Ethan Brooks",
                now.AddDays(-1), InvoiceStatus.Cancelled, 65m, 5.20m, 70.20m, "Cancelled along with the booking."),
            new Invoice("invoice-5", "customer-1", "Amelia Hart", "booking-1", "Colour Touch-Up - Amelia Hart",
                now.AddDays(-1), InvoiceStatus.PartiallyPaid, 146m, 11.68m, 157.68m, "Deposit collected; balance due at appointment."),
            new Invoice("invoice-6", "customer-3", "Sophia Reyes", "booking-2", "Full Package - Balayage & Style - Sophia Reyes",
                now, InvoiceStatus.Issued, 272m, 21.76m, 293.76m, "Awaiting deposit."),
            new Invoice("invoice-7", "customer-7", "Grace Kim", string.Empty, string.Empty,
                now.AddDays(-1), InvoiceStatus.Paid, 54m, 4.32m, 58.32m, "Retail walk-in sale."),
            new Invoice("invoice-8", "customer-5", "Olivia Chen", "booking-3", "Corporate Group Styling - Olivia Chen",
                now.AddDays(-2), InvoiceStatus.PartiallyPaid, 480m, 38.40m, 518.40m, "Deposit collected for team of six."),
        ];

        _items =
        [
            new InvoiceItem("line-1", "invoice-1", string.Empty, "service-4", "Manicure", 1, 40m, 40m),
            new InvoiceItem("line-2", "invoice-1", "product-6", string.Empty, "Gel Polish - Rose Quartz", 1, 9m, 9m),
            new InvoiceItem("line-3", "invoice-1", "product-7", string.Empty, "Base & Top Coat Duo", 1, 15m, 15m),

            new InvoiceItem("line-4", "invoice-2", string.Empty, "service-5", "Facial Renewal", 1, 85m, 85m),
            new InvoiceItem("line-5", "invoice-2", "product-8", string.Empty, "Renewal Clay Mask 250ml", 1, 22m, 22m),

            new InvoiceItem("line-6", "invoice-3", string.Empty, "service-1", "Haircut & Style", 1, 65m, 65m),
            new InvoiceItem("line-7", "invoice-3", "product-1", string.Empty, "Hydrating Shampoo 1L", 1, 18m, 18m),

            new InvoiceItem("line-8", "invoice-4", string.Empty, "service-1", "Haircut & Style", 1, 65m, 65m),

            new InvoiceItem("line-9", "invoice-5", string.Empty, "service-2", "Colour Touch-Up", 1, 120m, 120m),
            new InvoiceItem("line-10", "invoice-5", "product-3", string.Empty, "Colour Developer 20 Vol", 1, 12m, 12m),
            new InvoiceItem("line-11", "invoice-5", "product-4", string.Empty, "Permanent Hair Colour - Chocolate Brown", 1, 14m, 14m),

            new InvoiceItem("line-12", "invoice-6", string.Empty, "service-3", "Full Package - Balayage & Style", 1, 220m, 220m),
            new InvoiceItem("line-13", "invoice-6", "product-5", string.Empty, "Lightening Powder 500g", 2, 26m, 52m),

            new InvoiceItem("line-14", "invoice-7", "product-2", string.Empty, "Repair Conditioner 1L", 2, 19m, 38m),
            new InvoiceItem("line-15", "invoice-7", "product-9", string.Empty, "Aromatherapy Massage Oil 500ml", 1, 16m, 16m),

            new InvoiceItem("line-16", "invoice-8", string.Empty, string.Empty, "Corporate Group Styling (6 stylists)", 6, 80m, 480m),
        ];

        _payments =
        [
            new Payment("payment-1", "invoice-1", "customer-1", "Amelia Hart", PaymentMethod.Cash, 69.12m, now.AddDays(-5), string.Empty, "Paid in full at checkout."),
            new Payment("payment-2", "invoice-2", "customer-3", "Sophia Reyes", PaymentMethod.Card, 115.56m, now.AddDays(-10), string.Empty, "Paid in full at checkout."),
            new Payment("payment-3", "invoice-3", "customer-4", "Liam Foster", PaymentMethod.Cash, 89.64m, now.AddDays(-95), string.Empty, "Paid in full at checkout."),
            new Payment("payment-4", "invoice-5", "customer-1", "Amelia Hart", PaymentMethod.Cash, 50.00m, now.AddDays(-1), "session-1", "Deposit taken ahead of appointment."),
            new Payment("payment-5", "invoice-7", "customer-7", "Grace Kim", PaymentMethod.Card, 58.32m, now.AddDays(-1), "session-1", "Retail walk-in sale."),
            new Payment("payment-6", "invoice-8", "customer-5", "Olivia Chen", PaymentMethod.Cash, 200.00m, now.AddDays(-2), "session-1", "Deposit for corporate booking."),
        ];

        _receipts =
        [
            new Receipt("receipt-1", "payment-1", "invoice-1", now.AddDays(-5), 69.12m, "Amelia Hart"),
            new Receipt("receipt-2", "payment-2", "invoice-2", now.AddDays(-10), 115.56m, "Sophia Reyes"),
            new Receipt("receipt-3", "payment-3", "invoice-3", now.AddDays(-95), 89.64m, "Liam Foster"),
            new Receipt("receipt-4", "payment-4", "invoice-5", now.AddDays(-1), 50.00m, "Amelia Hart"),
            new Receipt("receipt-5", "payment-5", "invoice-7", now.AddDays(-1), 58.32m, "Grace Kim"),
            new Receipt("receipt-6", "payment-6", "invoice-8", now.AddDays(-2), 200.00m, "Olivia Chen"),
        ];

        _cashSessions =
        [
            new CashSession("session-1", "Priya Nair", now.AddDays(-2), now.AddDays(-1), 200m, 508.32m, CashSessionStatus.Closed),
            new CashSession("session-2", "Jordan Lee", now.AddHours(-3), null, 250m, null, CashSessionStatus.Open),
        ];
    }

    public async Task<IReadOnlyList<Invoice>> GetInvoicesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(400, cancellationToken).ConfigureAwait(true);
        return _invoices.ToList();
    }

    public async Task<Invoice?> GetInvoiceByIdAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _invoices.FirstOrDefault(invoice => invoice.Id == invoiceId);
    }

    public async Task<Invoice> CreateInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _invoices.Add(invoice);
        return invoice;
    }

    public async Task<Invoice> UpdateInvoiceStatusAsync(string invoiceId, InvoiceStatus status, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        var index = _invoices.FindIndex(invoice => invoice.Id == invoiceId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Invoice '{invoiceId}' was not found.");
        }

        var updated = _invoices[index] with { Status = status };
        _invoices[index] = updated;
        return updated;
    }

    public async Task<IReadOnlyList<InvoiceItem>> GetInvoiceItemsAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _items.Where(item => item.InvoiceId == invoiceId).ToList();
    }

    public async Task<InvoiceItem> AddInvoiceItemAsync(InvoiceItem item, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _items.Add(item);
        return item;
    }

    public async Task<IReadOnlyList<Payment>> GetPaymentsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(400, cancellationToken).ConfigureAwait(true);
        return _payments.ToList();
    }

    public async Task<IReadOnlyList<Payment>> GetPaymentsForInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _payments.Where(payment => payment.InvoiceId == invoiceId).ToList();
    }

    public async Task<Payment> RecordPaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _payments.Add(payment);
        return payment;
    }

    public async Task<Receipt> CreateReceiptAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _receipts.Add(receipt);
        return receipt;
    }

    public async Task<IReadOnlyList<Receipt>> GetReceiptsForInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _receipts.Where(receipt => receipt.InvoiceId == invoiceId).ToList();
    }

    public async Task<IReadOnlyList<CashSession>> GetCashSessionsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _cashSessions.ToList();
    }

    public async Task<CashSession?> GetOpenCashSessionAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _cashSessions.FirstOrDefault(session => session.Status == CashSessionStatus.Open);
    }

    public async Task<CashSession> OpenCashSessionAsync(CashSession session, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _cashSessions.Add(session);
        return session;
    }

    public async Task<CashSession> CloseCashSessionAsync(string sessionId, decimal closingBalance, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        var index = _cashSessions.FindIndex(session => session.Id == sessionId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Cash session '{sessionId}' was not found.");
        }

        var updated = _cashSessions[index] with { ClosedAt = DateTimeOffset.Now, ClosingBalance = closingBalance, Status = CashSessionStatus.Closed };
        _cashSessions[index] = updated;
        return updated;
    }
}
