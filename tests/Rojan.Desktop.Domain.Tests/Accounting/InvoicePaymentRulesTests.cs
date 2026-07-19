using Rojan.Desktop.Domain.Accounting;

namespace Rojan.Desktop.Domain.Tests.Accounting;

public sealed class InvoicePaymentRulesTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(50)]
    public void IsValidPaymentAmount_PositiveAmount_ReturnsTrue(decimal amount)
    {
        Assert.True(InvoicePaymentRules.IsValidPaymentAmount(amount));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void IsValidPaymentAmount_ZeroOrNegativeAmount_ReturnsFalse(decimal amount)
    {
        Assert.False(InvoicePaymentRules.IsValidPaymentAmount(amount));
    }

    [Fact]
    public void DetermineStatus_NoPayment_ReturnsIssued()
    {
        var result = InvoicePaymentRules.DetermineStatus(100m, 0m);

        Assert.Equal(InvoiceStatus.Issued, result);
    }

    [Fact]
    public void DetermineStatus_PartialPayment_ReturnsPartiallyPaid()
    {
        var result = InvoicePaymentRules.DetermineStatus(100m, 40m);

        Assert.Equal(InvoiceStatus.PartiallyPaid, result);
    }

    [Fact]
    public void DetermineStatus_ExactPayment_ReturnsPaid()
    {
        var result = InvoicePaymentRules.DetermineStatus(100m, 100m);

        Assert.Equal(InvoiceStatus.Paid, result);
    }

    [Fact]
    public void DetermineStatus_Overpayment_ReturnsPaid()
    {
        var result = InvoicePaymentRules.DetermineStatus(100m, 120m);

        Assert.Equal(InvoiceStatus.Paid, result);
    }
}
