namespace Rojan.Desktop.Application.Accounting;

/// <summary>Application-layer shape of an invoice, mapped from <see cref="Rojan.Desktop.Domain.Accounting.Invoice"/> by <see cref="AccountingMapper"/>.</summary>
public sealed record InvoiceDto(
    string Id,
    string CustomerId,
    string CustomerName,
    string BookingId,
    string BookingReference,
    DateTimeOffset IssuedAt,
    InvoiceStatus Status,
    decimal Subtotal,
    decimal TaxAmount,
    decimal Total,
    string Notes);
