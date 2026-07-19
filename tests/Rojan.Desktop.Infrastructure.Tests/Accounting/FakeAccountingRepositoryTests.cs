using Rojan.Desktop.Domain.Accounting;
using Rojan.Desktop.Infrastructure.Accounting;

namespace Rojan.Desktop.Infrastructure.Tests.Accounting;

/// <summary>Smoke + behavioral coverage - same reasoning as Inventory.FakeInventoryRepositoryTests, covering all five seeded Accounting aggregate types.</summary>
public sealed class FakeAccountingRepositoryTests
{
    [Fact]
    public async Task GetInvoicesAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeAccountingRepository();

        var result = await sut.GetInvoicesAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetInvoicesAsync_CancellationAlreadyRequested_ThrowsTaskCanceledException()
    {
        var sut = new FakeAccountingRepository();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => sut.GetInvoicesAsync(cts.Token));
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_KnownId_ReturnsMatchingInvoice()
    {
        var sut = new FakeAccountingRepository();

        var invoice = await sut.GetInvoiceByIdAsync("invoice-1");

        Assert.NotNull(invoice);
        Assert.Equal("invoice-1", invoice.Id);
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_UnknownId_ReturnsNull()
    {
        var sut = new FakeAccountingRepository();

        var invoice = await sut.GetInvoiceByIdAsync("no-such-invoice");

        Assert.Null(invoice);
    }

    [Fact]
    public async Task CreateInvoiceAsync_NewInvoice_BecomesVisibleViaGetInvoicesAsync()
    {
        var sut = new FakeAccountingRepository();
        var invoice = new Invoice("invoice-new", "customer-1", "Amelia Hart", string.Empty, string.Empty, DateTimeOffset.Now, InvoiceStatus.Issued, 40m, 3.20m, 43.20m, string.Empty);

        await sut.CreateInvoiceAsync(invoice);
        var invoices = await sut.GetInvoicesAsync();

        Assert.Contains(invoices, i => i.Id == "invoice-new");
    }

    [Fact]
    public async Task UpdateInvoiceStatusAsync_ExistingInvoice_ChangesStatus()
    {
        var sut = new FakeAccountingRepository();

        var updated = await sut.UpdateInvoiceStatusAsync("invoice-6", InvoiceStatus.Cancelled);
        var reloaded = await sut.GetInvoiceByIdAsync("invoice-6");

        Assert.Equal(InvoiceStatus.Cancelled, updated.Status);
        Assert.Equal(InvoiceStatus.Cancelled, reloaded!.Status);
    }

    [Fact]
    public async Task UpdateInvoiceStatusAsync_UnknownInvoice_ThrowsInvalidOperationException()
    {
        var sut = new FakeAccountingRepository();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateInvoiceStatusAsync("no-such-invoice", InvoiceStatus.Cancelled));
    }

    [Fact]
    public async Task GetInvoiceItemsAsync_KnownInvoice_ReturnsOnlyThatInvoicesItems()
    {
        var sut = new FakeAccountingRepository();

        var items = await sut.GetInvoiceItemsAsync("invoice-1");

        Assert.NotEmpty(items);
        Assert.All(items, item => Assert.Equal("invoice-1", item.InvoiceId));
    }

    [Fact]
    public async Task AddInvoiceItemAsync_NewItem_BecomesVisibleViaGetInvoiceItemsAsync()
    {
        var sut = new FakeAccountingRepository();
        var item = new InvoiceItem("line-new", "invoice-1", "product-1", string.Empty, "Hydrating Shampoo 1L", 1, 18m, 18m);

        await sut.AddInvoiceItemAsync(item);
        var items = await sut.GetInvoiceItemsAsync("invoice-1");

        Assert.Contains(items, i => i.Id == "line-new");
    }

    [Fact]
    public async Task GetPaymentsAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeAccountingRepository();

        var result = await sut.GetPaymentsAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetPaymentsForInvoiceAsync_KnownInvoice_ReturnsOnlyThatInvoicesPayments()
    {
        var sut = new FakeAccountingRepository();

        var payments = await sut.GetPaymentsForInvoiceAsync("invoice-1");

        Assert.NotEmpty(payments);
        Assert.All(payments, payment => Assert.Equal("invoice-1", payment.InvoiceId));
    }

    [Fact]
    public async Task RecordPaymentAsync_NewPayment_BecomesVisibleViaGetPaymentsForInvoiceAsync()
    {
        var sut = new FakeAccountingRepository();
        var payment = new Payment("payment-new", "invoice-6", "customer-3", "Sophia Reyes", PaymentMethod.Cash, 50m, DateTimeOffset.Now, string.Empty, string.Empty);

        await sut.RecordPaymentAsync(payment);
        var payments = await sut.GetPaymentsForInvoiceAsync("invoice-6");

        Assert.Contains(payments, p => p.Id == "payment-new");
    }

    [Fact]
    public async Task CreateReceiptAsync_NewReceipt_BecomesVisibleViaGetReceiptsForInvoiceAsync()
    {
        var sut = new FakeAccountingRepository();
        var receipt = new Receipt("receipt-new", "payment-1", "invoice-1", DateTimeOffset.Now, 69.12m, "Amelia Hart");

        await sut.CreateReceiptAsync(receipt);
        var receipts = await sut.GetReceiptsForInvoiceAsync("invoice-1");

        Assert.Contains(receipts, r => r.Id == "receipt-new");
    }

    [Fact]
    public async Task GetReceiptsForInvoiceAsync_KnownInvoice_ReturnsOnlyThatInvoicesReceipts()
    {
        var sut = new FakeAccountingRepository();

        var receipts = await sut.GetReceiptsForInvoiceAsync("invoice-1");

        Assert.NotEmpty(receipts);
        Assert.All(receipts, receipt => Assert.Equal("invoice-1", receipt.InvoiceId));
    }

    [Fact]
    public async Task GetCashSessionsAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeAccountingRepository();

        var result = await sut.GetCashSessionsAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetOpenCashSessionAsync_ReturnsTheOpenSeededSession()
    {
        var sut = new FakeAccountingRepository();

        var session = await sut.GetOpenCashSessionAsync();

        Assert.NotNull(session);
        Assert.Equal(CashSessionStatus.Open, session.Status);
    }

    [Fact]
    public async Task OpenCashSessionAsync_NewSession_BecomesVisibleViaGetCashSessionsAsync()
    {
        var sut = new FakeAccountingRepository();
        var session = new CashSession("session-new", "Casey Morgan", DateTimeOffset.Now, null, 100m, null, CashSessionStatus.Open);

        await sut.OpenCashSessionAsync(session);
        var sessions = await sut.GetCashSessionsAsync();

        Assert.Contains(sessions, s => s.Id == "session-new");
    }

    [Fact]
    public async Task CloseCashSessionAsync_ExistingSession_SetsClosedStatusAndBalance()
    {
        var sut = new FakeAccountingRepository();

        var closed = await sut.CloseCashSessionAsync("session-1", 999m);

        Assert.Equal(CashSessionStatus.Closed, closed.Status);
        Assert.Equal(999m, closed.ClosingBalance);
    }

    [Fact]
    public async Task CloseCashSessionAsync_UnknownSession_ThrowsInvalidOperationException()
    {
        var sut = new FakeAccountingRepository();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CloseCashSessionAsync("no-such-session", 100m));
    }
}
