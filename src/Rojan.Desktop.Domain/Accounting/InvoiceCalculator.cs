namespace Rojan.Desktop.Domain.Accounting;

/// <summary>
/// How an invoice's totals are computed from its line items - a genuine
/// Domain rule (line total = quantity x unit price, subtotal = sum of
/// line totals, tax = subtotal x rate, total = subtotal + tax), not
/// something Application/Presentation should each reimplement. Rounds to
/// 2 decimal places at the tax step only - line totals and the subtotal
/// stay exact until then, so rounding error can't compound across items.
/// </summary>
public static class InvoiceCalculator
{
    public static decimal ComputeLineTotal(int quantity, decimal unitPrice) => quantity * unitPrice;

    public static decimal ComputeSubtotal(IEnumerable<InvoiceItem> items) => items.Sum(item => item.LineTotal);

    public static decimal ComputeTax(decimal subtotal, decimal taxRate) => Math.Round(subtotal * taxRate, 2, MidpointRounding.AwayFromZero);

    public static decimal ComputeTotal(decimal subtotal, decimal taxAmount) => subtotal + taxAmount;
}
