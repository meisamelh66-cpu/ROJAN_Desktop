namespace Rojan.Desktop.Domain.Accounting;

/// <summary>A single payment applied against an <see cref="Invoice"/>, as returned by <see cref="IAccountingRepository"/>. <see cref="CashSessionId"/> is empty for non-cash methods.</summary>
public sealed record Payment(
    string Id,
    string InvoiceId,
    string CustomerId,
    string CustomerName,
    PaymentMethod Method,
    decimal Amount,
    DateTimeOffset PaidAt,
    string CashSessionId,
    string Notes);
