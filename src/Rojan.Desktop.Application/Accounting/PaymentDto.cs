namespace Rojan.Desktop.Application.Accounting;

/// <summary>Application-layer shape of a payment, mapped from <see cref="Rojan.Desktop.Domain.Accounting.Payment"/> by <see cref="AccountingMapper"/>.</summary>
public sealed record PaymentDto(
    string Id,
    string InvoiceId,
    string CustomerId,
    string CustomerName,
    PaymentMethod Method,
    decimal Amount,
    DateTimeOffset PaidAt,
    string CashSessionId,
    string Notes);
