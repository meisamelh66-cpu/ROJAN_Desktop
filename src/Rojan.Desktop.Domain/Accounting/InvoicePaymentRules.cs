namespace Rojan.Desktop.Domain.Accounting;

/// <summary>
/// How an invoice's <see cref="InvoiceStatus"/> follows from its payments -
/// a genuine Domain rule, not something Application/Presentation should
/// each reimplement, same reasoning as <c>Inventory.StockTransactionRules</c>.
/// </summary>
public static class InvoicePaymentRules
{
    public static bool IsValidPaymentAmount(decimal amount) => amount > 0;

    /// <summary>An invoice with no payments stays Issued; a positive but incomplete total is PartiallyPaid; a total at or above the invoice total is Paid. Cancelled invoices are terminal and are never re-derived by this rule - callers must not call this for a Cancelled invoice.</summary>
    public static InvoiceStatus DetermineStatus(decimal invoiceTotal, decimal totalPaid) => totalPaid switch
    {
        <= 0 => InvoiceStatus.Issued,
        var paid when paid < invoiceTotal => InvoiceStatus.PartiallyPaid,
        _ => InvoiceStatus.Paid,
    };
}
