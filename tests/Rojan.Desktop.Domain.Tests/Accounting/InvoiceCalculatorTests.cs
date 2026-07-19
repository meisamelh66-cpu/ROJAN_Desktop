using Rojan.Desktop.Domain.Accounting;

namespace Rojan.Desktop.Domain.Tests.Accounting;

public sealed class InvoiceCalculatorTests
{
    [Fact]
    public void ComputeLineTotal_MultipliesQuantityAndUnitPrice()
    {
        var result = InvoiceCalculator.ComputeLineTotal(3, 15m);

        Assert.Equal(45m, result);
    }

    [Fact]
    public void ComputeSubtotal_SumsAllLineTotals()
    {
        var items = new List<InvoiceItem>
        {
            new("line-1", "invoice-1", string.Empty, "service-4", "Manicure", 1, 40m, 40m),
            new("line-2", "invoice-1", "product-6", string.Empty, "Gel Polish", 1, 9m, 9m),
            new("line-3", "invoice-1", "product-7", string.Empty, "Base & Top Coat Duo", 1, 15m, 15m),
        };

        var result = InvoiceCalculator.ComputeSubtotal(items);

        Assert.Equal(64m, result);
    }

    [Fact]
    public void ComputeTax_RoundsToTwoDecimalPlacesAwayFromZero()
    {
        var result = InvoiceCalculator.ComputeTax(107m, 0.08m);

        Assert.Equal(8.56m, result);
    }

    [Fact]
    public void ComputeTax_MidpointRoundsAwayFromZero()
    {
        var result = InvoiceCalculator.ComputeTax(0.125m, 1m);

        Assert.Equal(0.13m, result);
    }

    [Fact]
    public void ComputeTotal_AddsSubtotalAndTax()
    {
        var result = InvoiceCalculator.ComputeTotal(107m, 8.56m);

        Assert.Equal(115.56m, result);
    }
}
