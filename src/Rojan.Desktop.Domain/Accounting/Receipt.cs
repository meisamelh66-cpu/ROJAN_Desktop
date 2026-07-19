namespace Rojan.Desktop.Domain.Accounting;

/// <summary>The proof-of-payment record generated when a <see cref="Payment"/> completes, as returned by <see cref="IAccountingRepository"/>.</summary>
public sealed record Receipt(
    string Id,
    string PaymentId,
    string InvoiceId,
    DateTimeOffset IssuedAt,
    decimal AmountPaid,
    string CustomerName);
