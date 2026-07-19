namespace Rojan.Desktop.Domain.Accounting;

/// <summary>Lifecycle stage of an invoice, as returned by <see cref="IAccountingRepository"/> - driven by <see cref="InvoicePaymentRules.DetermineStatus"/> as payments are recorded, not set directly by callers once issued.</summary>
public enum InvoiceStatus
{
    Issued,
    PartiallyPaid,
    Paid,
    Cancelled,
}
