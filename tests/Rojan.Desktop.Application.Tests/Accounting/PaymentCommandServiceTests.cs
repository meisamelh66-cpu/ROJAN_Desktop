using Rojan.Desktop.Application.Accounting;
using DomainAccounting = Rojan.Desktop.Domain.Accounting;

namespace Rojan.Desktop.Application.Tests.Accounting;

public sealed class PaymentCommandServiceTests
{
    private static PaymentCommandService MakeSut(StubAccountingRepository? repository = null) =>
        new(repository ?? new StubAccountingRepository());

    private static DomainAccounting.Invoice MakeInvoice(string id, decimal total, DomainAccounting.InvoiceStatus status = DomainAccounting.InvoiceStatus.Issued) =>
        new(id, "customer-1", "Amelia Hart", string.Empty, string.Empty, DateTimeOffset.UnixEpoch, status, total - 8m, 8m, total, string.Empty);

    [Fact]
    public async Task RecordPaymentAsync_InvalidAmount_ThrowsArgumentException()
    {
        var sut = MakeSut();
        var request = new RecordPaymentRequest("invoice-1", PaymentMethod.Cash, 0m, string.Empty, string.Empty);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.RecordPaymentAsync(request));
    }

    [Fact]
    public async Task RecordPaymentAsync_UnknownInvoice_ThrowsInvalidOperationException()
    {
        var sut = MakeSut();
        var request = new RecordPaymentRequest("no-such-invoice", PaymentMethod.Cash, 40m, string.Empty, string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RecordPaymentAsync(request));
    }

    [Fact]
    public async Task RecordPaymentAsync_CancelledInvoice_ThrowsInvalidOperationException()
    {
        var repository = new StubAccountingRepository();
        repository.Invoices.Add(MakeInvoice("invoice-1", 100m, DomainAccounting.InvoiceStatus.Cancelled));
        var sut = MakeSut(repository);
        var request = new RecordPaymentRequest("invoice-1", PaymentMethod.Cash, 40m, string.Empty, string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RecordPaymentAsync(request));
    }

    [Fact]
    public async Task RecordPaymentAsync_PartialAmount_SetsInvoiceStatusToPartiallyPaid()
    {
        var repository = new StubAccountingRepository();
        repository.Invoices.Add(MakeInvoice("invoice-1", 100m));
        var sut = MakeSut(repository);
        var request = new RecordPaymentRequest("invoice-1", PaymentMethod.Cash, 40m, string.Empty, string.Empty);

        var payment = await sut.RecordPaymentAsync(request);

        Assert.Equal(40m, payment.Amount);
        Assert.Equal(DomainAccounting.InvoiceStatus.PartiallyPaid, repository.Invoices.Single().Status);
        Assert.Single(repository.Payments);
        Assert.Single(repository.Receipts);
    }

    [Fact]
    public async Task RecordPaymentAsync_FullAmount_SetsInvoiceStatusToPaid()
    {
        var repository = new StubAccountingRepository();
        repository.Invoices.Add(MakeInvoice("invoice-1", 100m));
        var sut = MakeSut(repository);
        var request = new RecordPaymentRequest("invoice-1", PaymentMethod.Cash, 100m, string.Empty, string.Empty);

        await sut.RecordPaymentAsync(request);

        Assert.Equal(DomainAccounting.InvoiceStatus.Paid, repository.Invoices.Single().Status);
    }

    [Fact]
    public async Task RecordPaymentAsync_SecondPaymentCompletesBalance_SetsInvoiceStatusToPaid()
    {
        var repository = new StubAccountingRepository();
        repository.Invoices.Add(MakeInvoice("invoice-1", 100m));
        repository.Payments.Add(new DomainAccounting.Payment("payment-1", "invoice-1", "customer-1", "Amelia Hart", DomainAccounting.PaymentMethod.Cash, 40m, DateTimeOffset.UnixEpoch, string.Empty, string.Empty));
        var sut = MakeSut(repository);
        var request = new RecordPaymentRequest("invoice-1", PaymentMethod.Cash, 60m, string.Empty, string.Empty);

        await sut.RecordPaymentAsync(request);

        Assert.Equal(DomainAccounting.InvoiceStatus.Paid, repository.Invoices.Single().Status);
        Assert.Equal(2, repository.Payments.Count);
    }

    [Fact]
    public async Task OpenCashSessionAsync_NoOpenSession_OpensNewSession()
    {
        var repository = new StubAccountingRepository();
        var sut = MakeSut(repository);

        var session = await sut.OpenCashSessionAsync("Jordan Lee", 200m);

        Assert.Equal(CashSessionStatus.Open, session.Status);
        Assert.Equal(200m, session.OpeningFloat);
        Assert.Single(repository.CashSessions);
    }

    [Fact]
    public async Task OpenCashSessionAsync_SessionAlreadyOpen_ThrowsInvalidOperationException()
    {
        var repository = new StubAccountingRepository();
        repository.CashSessions.Add(new DomainAccounting.CashSession("session-1", "Jordan Lee", DateTimeOffset.UnixEpoch, null, 200m, null, DomainAccounting.CashSessionStatus.Open));
        var sut = MakeSut(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.OpenCashSessionAsync("Priya Nair", 150m));
    }

    [Fact]
    public async Task CloseCashSessionAsync_OpenSession_ClosesWithBalance()
    {
        var repository = new StubAccountingRepository();
        repository.CashSessions.Add(new DomainAccounting.CashSession("session-1", "Jordan Lee", DateTimeOffset.UnixEpoch, null, 200m, null, DomainAccounting.CashSessionStatus.Open));
        var sut = MakeSut(repository);

        var closed = await sut.CloseCashSessionAsync("session-1", 350m);

        Assert.Equal(CashSessionStatus.Closed, closed.Status);
        Assert.Equal(350m, closed.ClosingBalance);
    }
}
