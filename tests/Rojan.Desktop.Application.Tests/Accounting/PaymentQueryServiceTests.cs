using Rojan.Desktop.Application.Accounting;
using DomainAccounting = Rojan.Desktop.Domain.Accounting;

namespace Rojan.Desktop.Application.Tests.Accounting;

public sealed class PaymentQueryServiceTests
{
    private static PaymentQueryService MakeSut(StubAccountingRepository? repository = null) =>
        new(repository ?? new StubAccountingRepository());

    [Fact]
    public async Task GetPaymentsForInvoiceAsync_ReturnsOnlyThatInvoicesPayments()
    {
        var repository = new StubAccountingRepository();
        repository.Payments.Add(new DomainAccounting.Payment("payment-1", "invoice-1", "customer-1", "Amelia Hart", DomainAccounting.PaymentMethod.Cash, 40m, DateTimeOffset.UnixEpoch, string.Empty, string.Empty));
        repository.Payments.Add(new DomainAccounting.Payment("payment-2", "invoice-2", "customer-2", "Noah Bennett", DomainAccounting.PaymentMethod.Card, 20m, DateTimeOffset.UnixEpoch, string.Empty, string.Empty));
        var sut = MakeSut(repository);

        var result = await sut.GetPaymentsForInvoiceAsync("invoice-1");

        var payment = Assert.Single(result);
        Assert.Equal("payment-1", payment.Id);
    }

    [Fact]
    public async Task GetRevenueSummaryAsync_ComputesTotalsAcrossInvoicesAndPayments()
    {
        var repository = new StubAccountingRepository();
        var today = DateTimeOffset.Now;

        // Fully paid invoice.
        repository.Invoices.Add(new DomainAccounting.Invoice("invoice-1", "customer-1", "Amelia Hart", string.Empty, string.Empty, today, DomainAccounting.InvoiceStatus.Paid, 40m, 3.20m, 43.20m, string.Empty));
        repository.Payments.Add(new DomainAccounting.Payment("payment-1", "invoice-1", "customer-1", "Amelia Hart", DomainAccounting.PaymentMethod.Cash, 43.20m, today, string.Empty, string.Empty));

        // Partially paid invoice, issued in the past (not "today").
        repository.Invoices.Add(new DomainAccounting.Invoice("invoice-2", "customer-2", "Sophia Reyes", string.Empty, string.Empty, today.AddDays(-10), DomainAccounting.InvoiceStatus.PartiallyPaid, 100m, 8m, 108m, string.Empty));
        repository.Payments.Add(new DomainAccounting.Payment("payment-2", "invoice-2", "customer-2", "Sophia Reyes", DomainAccounting.PaymentMethod.Cash, 50m, today.AddDays(-10), string.Empty, string.Empty));

        // Issued invoice with no payment yet.
        repository.Invoices.Add(new DomainAccounting.Invoice("invoice-3", "customer-3", "Liam Foster", string.Empty, string.Empty, today, DomainAccounting.InvoiceStatus.Issued, 65m, 5.20m, 70.20m, string.Empty));

        var sut = MakeSut(repository);

        var summary = await sut.GetRevenueSummaryAsync();

        Assert.Equal(93.20m, summary.TotalRevenue);
        Assert.Equal(43.20m, summary.TodayRevenue);
        Assert.Equal(58m + 70.20m, summary.OutstandingBalance);
        Assert.Equal(1, summary.PaidInvoiceCount);
        Assert.Equal(2, summary.OutstandingInvoiceCount);
    }

    [Fact]
    public async Task GetOpenCashSessionAsync_NoOpenSession_ReturnsNull()
    {
        var sut = MakeSut();

        var result = await sut.GetOpenCashSessionAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOpenCashSessionAsync_OpenSessionExists_ReturnsIt()
    {
        var repository = new StubAccountingRepository();
        repository.CashSessions.Add(new DomainAccounting.CashSession("session-1", "Jordan Lee", DateTimeOffset.UnixEpoch, null, 200m, null, DomainAccounting.CashSessionStatus.Open));
        var sut = MakeSut(repository);

        var result = await sut.GetOpenCashSessionAsync();

        Assert.NotNull(result);
        Assert.Equal("session-1", result.Id);
    }
}
