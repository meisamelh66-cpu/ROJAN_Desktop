using Rojan.Desktop.Domain.Accounting;

namespace Rojan.Desktop.Domain.Tests.Accounting;

/// <summary>Minimal smoke coverage - see the equivalent note on Customers.CustomerTests for why Domain testing stays light.</summary>
public sealed class AccountingRecordsTests
{
    [Fact]
    public void Invoice_SameValues_AreEqual()
    {
        var issuedAt = DateTimeOffset.UnixEpoch;
        var first = new Invoice("invoice-1", "customer-1", "Amelia Hart", "booking-1", "Manicure - Amelia Hart", issuedAt, InvoiceStatus.Issued, 40m, 3.20m, 43.20m, string.Empty);
        var second = new Invoice("invoice-1", "customer-1", "Amelia Hart", "booking-1", "Manicure - Amelia Hart", issuedAt, InvoiceStatus.Issued, 40m, 3.20m, 43.20m, string.Empty);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Invoice_DifferentStatus_AreNotEqual()
    {
        var first = new Invoice("invoice-1", "customer-1", "Amelia Hart", "booking-1", "Manicure - Amelia Hart", DateTimeOffset.UnixEpoch, InvoiceStatus.Issued, 40m, 3.20m, 43.20m, string.Empty);
        var second = first with { Status = InvoiceStatus.Paid };

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void InvoiceItem_DifferentQuantity_AreNotEqual()
    {
        var first = new InvoiceItem("line-1", "invoice-1", string.Empty, "service-4", "Manicure", 1, 40m, 40m);
        var second = first with { Quantity = 2, LineTotal = 80m };

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Payment_SameValues_AreEqual()
    {
        var paidAt = DateTimeOffset.UnixEpoch;
        var first = new Payment("payment-1", "invoice-1", "customer-1", "Amelia Hart", PaymentMethod.Cash, 43.20m, paidAt, string.Empty, string.Empty);
        var second = new Payment("payment-1", "invoice-1", "customer-1", "Amelia Hart", PaymentMethod.Cash, 43.20m, paidAt, string.Empty, string.Empty);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Payment_DifferentMethod_AreNotEqual()
    {
        var first = new Payment("payment-1", "invoice-1", "customer-1", "Amelia Hart", PaymentMethod.Cash, 43.20m, DateTimeOffset.UnixEpoch, string.Empty, string.Empty);
        var second = first with { Method = PaymentMethod.Card };

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Receipt_DifferentAmount_AreNotEqual()
    {
        var first = new Receipt("receipt-1", "payment-1", "invoice-1", DateTimeOffset.UnixEpoch, 43.20m, "Amelia Hart");
        var second = first with { AmountPaid = 20m };

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void CashSession_DifferentStatus_AreNotEqual()
    {
        var first = new CashSession("session-1", "Jordan Lee", DateTimeOffset.UnixEpoch, null, 200m, null, CashSessionStatus.Open);
        var second = first with { ClosedAt = DateTimeOffset.UnixEpoch, ClosingBalance = 250m, Status = CashSessionStatus.Closed };

        Assert.NotEqual(first, second);
    }
}
