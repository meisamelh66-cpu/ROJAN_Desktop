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
            new Invoice("invoice-1", "customer-1", "لیلا احمدی", "booking-5", "مانیکور - لیلا احمدی",
                now.AddDays(-5), InvoiceStatus.Paid, 640000m, 51200m, 691200m, string.Empty),
            new Invoice("invoice-2", "customer-3", "سارا محمدی", "booking-6", "فیشیال ترمیمی - سارا محمدی",
                now.AddDays(-10), InvoiceStatus.Paid, 1070000m, 85600m, 1155600m, string.Empty),
            new Invoice("invoice-3", "customer-4", "علی کریمی", "booking-7", "کوتاهی و استایل مو - علی کریمی",
                now.AddDays(-95), InvoiceStatus.Paid, 830000m, 66400m, 896400m, string.Empty),
            new Invoice("invoice-4", "customer-6", "رضا نوری", "booking-8", "کوتاهی و استایل مو - رضا نوری",
                now.AddDays(-1), InvoiceStatus.Cancelled, 650000m, 52000m, 702000m, "همراه با لغو نوبت، لغو شد."),
            new Invoice("invoice-5", "customer-1", "لیلا احمدی", "booking-1", "اصلاح رنگ ریشه - لیلا احمدی",
                now.AddDays(-1), InvoiceStatus.PartiallyPaid, 1460000m, 116800m, 1576800m, "بیعانه دریافت شد؛ باقیمانده هنگام نوبت پرداخت می‌شود."),
            new Invoice("invoice-6", "customer-3", "سارا محمدی", "booking-2", "پکیج کامل - بالیاژ و استایل - سارا محمدی",
                now, InvoiceStatus.Issued, 2720000m, 217600m, 2937600m, "در انتظار دریافت بیعانه."),
            new Invoice("invoice-7", "customer-7", "نگار صادقی", string.Empty, string.Empty,
                now.AddDays(-1), InvoiceStatus.Paid, 540000m, 43200m, 583200m, "فروش حضوری بدون نوبت قبلی."),
            new Invoice("invoice-8", "customer-5", "مریم حسینی", "booking-3", "پکیج گروهی سازمانی - مریم حسینی",
                now.AddDays(-2), InvoiceStatus.PartiallyPaid, 4800000m, 384000m, 5184000m, "بیعانه برای تیم شش‌نفره دریافت شد."),
        ];

        _items =
        [
            new InvoiceItem("line-1", "invoice-1", string.Empty, "service-4", "مانیکور", 1, 400000m, 400000m),
            new InvoiceItem("line-2", "invoice-1", "product-6", string.Empty, "لاک ژل - کوارتز صورتی", 1, 90000m, 90000m),
            new InvoiceItem("line-3", "invoice-1", "product-7", string.Empty, "ست بیس و تاپ کوت", 1, 150000m, 150000m),

            new InvoiceItem("line-4", "invoice-2", string.Empty, "service-5", "فیشیال ترمیمی", 1, 850000m, 850000m),
            new InvoiceItem("line-5", "invoice-2", "product-8", string.Empty, "ماسک گلی ترمیمی ۲۵۰ میلی‌لیتری", 1, 220000m, 220000m),

            new InvoiceItem("line-6", "invoice-3", string.Empty, "service-1", "کوتاهی و استایل مو", 1, 650000m, 650000m),
            new InvoiceItem("line-7", "invoice-3", "product-1", string.Empty, "شامپو مرطوب‌کننده ۱ لیتری", 1, 180000m, 180000m),

            new InvoiceItem("line-8", "invoice-4", string.Empty, "service-1", "کوتاهی و استایل مو", 1, 650000m, 650000m),

            new InvoiceItem("line-9", "invoice-5", string.Empty, "service-2", "اصلاح رنگ ریشه", 1, 1200000m, 1200000m),
            new InvoiceItem("line-10", "invoice-5", "product-3", string.Empty, "اکسیدان رنگ ۲۰ ولوم", 1, 120000m, 120000m),
            new InvoiceItem("line-11", "invoice-5", "product-4", string.Empty, "رنگ دائم مو - قهوه‌ای شکلاتی", 1, 140000m, 140000m),

            new InvoiceItem("line-12", "invoice-6", string.Empty, "service-3", "پکیج کامل - بالیاژ و استایل", 1, 2200000m, 2200000m),
            new InvoiceItem("line-13", "invoice-6", "product-5", string.Empty, "پودر دکلره ۵۰۰ گرمی", 2, 260000m, 520000m),

            new InvoiceItem("line-14", "invoice-7", "product-2", string.Empty, "نرم‌کننده ترمیمی ۱ لیتری", 2, 190000m, 380000m),
            new InvoiceItem("line-15", "invoice-7", "product-9", string.Empty, "روغن ماساژ رایحه‌درمانی ۵۰۰ میلی‌لیتری", 1, 160000m, 160000m),

            new InvoiceItem("line-16", "invoice-8", string.Empty, string.Empty, "پکیج گروهی سازمانی (۶ نفر)", 6, 800000m, 4800000m),
        ];

        _payments =
        [
            new Payment("payment-1", "invoice-1", "customer-1", "لیلا احمدی", PaymentMethod.Cash, 691200m, now.AddDays(-5), string.Empty, "هنگام تسویه به‌طور کامل پرداخت شد."),
            new Payment("payment-2", "invoice-2", "customer-3", "سارا محمدی", PaymentMethod.Card, 1155600m, now.AddDays(-10), string.Empty, "هنگام تسویه به‌طور کامل پرداخت شد."),
            new Payment("payment-3", "invoice-3", "customer-4", "علی کریمی", PaymentMethod.Cash, 896400m, now.AddDays(-95), string.Empty, "هنگام تسویه به‌طور کامل پرداخت شد."),
            new Payment("payment-4", "invoice-5", "customer-1", "لیلا احمدی", PaymentMethod.Cash, 500000m, now.AddDays(-1), "session-1", "بیعانه پیش از نوبت دریافت شد."),
            new Payment("payment-5", "invoice-7", "customer-7", "نگار صادقی", PaymentMethod.Card, 583200m, now.AddDays(-1), "session-1", "فروش حضوری بدون نوبت قبلی."),
            new Payment("payment-6", "invoice-8", "customer-5", "مریم حسینی", PaymentMethod.Cash, 2000000m, now.AddDays(-2), "session-1", "بیعانه برای نوبت سازمانی."),
        ];

        _receipts =
        [
            new Receipt("receipt-1", "payment-1", "invoice-1", now.AddDays(-5), 691200m, "لیلا احمدی"),
            new Receipt("receipt-2", "payment-2", "invoice-2", now.AddDays(-10), 1155600m, "سارا محمدی"),
            new Receipt("receipt-3", "payment-3", "invoice-3", now.AddDays(-95), 896400m, "علی کریمی"),
            new Receipt("receipt-4", "payment-4", "invoice-5", now.AddDays(-1), 500000m, "لیلا احمدی"),
            new Receipt("receipt-5", "payment-5", "invoice-7", now.AddDays(-1), 583200m, "نگار صادقی"),
            new Receipt("receipt-6", "payment-6", "invoice-8", now.AddDays(-2), 2000000m, "مریم حسینی"),
        ];

        _cashSessions =
        [
            new CashSession("session-1", "سارا امینی", now.AddDays(-2), now.AddDays(-1), 2000000m, 5083200m, CashSessionStatus.Closed),
            new CashSession("session-2", "کیانا رادمنش", now.AddHours(-3), null, 2500000m, null, CashSessionStatus.Open),
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
