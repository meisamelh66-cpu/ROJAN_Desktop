namespace Rojan.Desktop.Application.Accounting;

/// <summary>Application-layer shape of an invoice line item, mapped from <see cref="Rojan.Desktop.Domain.Accounting.InvoiceItem"/> by <see cref="AccountingMapper"/>.</summary>
public sealed record InvoiceItemDto(
    string Id,
    string InvoiceId,
    string ProductId,
    string ServiceId,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
