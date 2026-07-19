namespace Rojan.Desktop.Application.Accounting;

/// <summary>Application-layer shape of a receipt, mapped from <see cref="Rojan.Desktop.Domain.Accounting.Receipt"/> by <see cref="AccountingMapper"/>.</summary>
public sealed record ReceiptDto(
    string Id,
    string PaymentId,
    string InvoiceId,
    DateTimeOffset IssuedAt,
    decimal AmountPaid,
    string CustomerName);
