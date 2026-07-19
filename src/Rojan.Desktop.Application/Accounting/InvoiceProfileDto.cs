namespace Rojan.Desktop.Application.Accounting;

/// <summary>Everything the invoice detail screen needs for one invoice - line items, payments, and receipts, fetched together as a single unit of work, same reasoning as Customers.CustomerProfileDto/Services.ServiceProfileDto.</summary>
public sealed record InvoiceProfileDto(
    InvoiceDto Invoice,
    IReadOnlyList<InvoiceItemDto> Items,
    IReadOnlyList<PaymentDto> Payments,
    IReadOnlyList<ReceiptDto> Receipts);
