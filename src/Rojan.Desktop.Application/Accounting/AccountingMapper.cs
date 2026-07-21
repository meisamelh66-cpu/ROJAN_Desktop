using System.Globalization;
using DomainAccounting = Rojan.Desktop.Domain.Accounting;

namespace Rojan.Desktop.Application.Accounting;

/// <summary>Domain&lt;-&gt;Application mapping shared by every Accounting use case - same reasoning as <c>Customers.CustomerMapper</c>.</summary>
internal static class AccountingMapper
{
    public static InvoiceDto MapInvoice(DomainAccounting.Invoice invoice) => new(
        invoice.Id,
        invoice.CustomerId,
        invoice.CustomerName,
        invoice.BookingId,
        invoice.BookingReference,
        invoice.IssuedAt,
        MapStatus(invoice.Status),
        invoice.Subtotal,
        invoice.TaxAmount,
        invoice.Total,
        invoice.Notes);

    public static InvoiceItemDto MapItem(DomainAccounting.InvoiceItem item) => new(
        item.Id, item.InvoiceId, item.ProductId, item.ServiceId, item.Description, item.Quantity, item.UnitPrice, item.LineTotal);

    public static PaymentDto MapPayment(DomainAccounting.Payment payment) => new(
        payment.Id, payment.InvoiceId, payment.CustomerId, payment.CustomerName,
        MapMethod(payment.Method), payment.Amount, payment.PaidAt, payment.CashSessionId, payment.Notes);

    public static ReceiptDto MapReceipt(DomainAccounting.Receipt receipt) => new(
        receipt.Id, receipt.PaymentId, receipt.InvoiceId, receipt.IssuedAt, receipt.AmountPaid, receipt.CustomerName);

    public static CashSessionDto MapCashSession(DomainAccounting.CashSession session) => new(
        session.Id, session.CashierName, session.OpenedAt, session.ClosedAt,
        session.OpeningFloat, session.ClosingBalance, MapSessionStatus(session.Status));

    public static InvoiceStatus MapStatus(DomainAccounting.InvoiceStatus status) => status switch
    {
        DomainAccounting.InvoiceStatus.Issued => InvoiceStatus.Issued,
        DomainAccounting.InvoiceStatus.PartiallyPaid => InvoiceStatus.PartiallyPaid,
        DomainAccounting.InvoiceStatus.Paid => InvoiceStatus.Paid,
        DomainAccounting.InvoiceStatus.Cancelled => InvoiceStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown domain invoice status."),
    };

    public static DomainAccounting.InvoiceStatus MapStatusToDomain(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Issued => DomainAccounting.InvoiceStatus.Issued,
        InvoiceStatus.PartiallyPaid => DomainAccounting.InvoiceStatus.PartiallyPaid,
        InvoiceStatus.Paid => DomainAccounting.InvoiceStatus.Paid,
        InvoiceStatus.Cancelled => DomainAccounting.InvoiceStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown application invoice status."),
    };

    public static PaymentMethod MapMethod(DomainAccounting.PaymentMethod method) => method switch
    {
        DomainAccounting.PaymentMethod.Cash => PaymentMethod.Cash,
        DomainAccounting.PaymentMethod.Card => PaymentMethod.Card,
        DomainAccounting.PaymentMethod.BankTransfer => PaymentMethod.BankTransfer,
        DomainAccounting.PaymentMethod.GiftCard => PaymentMethod.GiftCard,
        DomainAccounting.PaymentMethod.StoreCredit => PaymentMethod.StoreCredit,
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown domain payment method."),
    };

    public static DomainAccounting.PaymentMethod MapMethodToDomain(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => DomainAccounting.PaymentMethod.Cash,
        PaymentMethod.Card => DomainAccounting.PaymentMethod.Card,
        PaymentMethod.BankTransfer => DomainAccounting.PaymentMethod.BankTransfer,
        PaymentMethod.GiftCard => DomainAccounting.PaymentMethod.GiftCard,
        PaymentMethod.StoreCredit => DomainAccounting.PaymentMethod.StoreCredit,
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown application payment method."),
    };

    public static CashSessionStatus MapSessionStatus(DomainAccounting.CashSessionStatus status) => status switch
    {
        DomainAccounting.CashSessionStatus.Open => CashSessionStatus.Open,
        DomainAccounting.CashSessionStatus.Closed => CashSessionStatus.Closed,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown domain cash session status."),
    };

    /// <summary>
    /// Parses a cross-slice string-money value (e.g. <c>Inventory.ProductDto.UnitPrice</c>
    /// "$18", <c>Services.ServiceDto.Price</c> "$65") into a decimal for
    /// Accounting's own arithmetic - the one place this parsing happens,
    /// at the boundary where Accounting reads another slice's DTOs, not
    /// inside any other module's own code.
    /// </summary>
    public static decimal ParseMoney(string value)
    {
        if (value.Contains("رایگان", StringComparison.Ordinal))
        {
            return 0m;
        }

        var trimmed = value.TrimStart('$').Replace("تومان", string.Empty, StringComparison.Ordinal).Replace("﷼", string.Empty, StringComparison.Ordinal).Trim();
        return decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;
    }
}
