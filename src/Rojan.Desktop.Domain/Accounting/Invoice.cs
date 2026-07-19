namespace Rojan.Desktop.Domain.Accounting;

/// <summary>
/// A single invoice, as returned by <see cref="IAccountingRepository"/>.
/// <see cref="CustomerId"/>/<see cref="CustomerName"/> and
/// <see cref="BookingId"/>/<see cref="BookingReference"/> are free-form,
/// unvalidated references - this vertical slice deliberately does not
/// depend on <c>Domain.Customers</c> or <c>Domain.Bookings</c> (per the
/// Independence goal in docs/architecture/00-overview.md §2);
/// <see cref="BookingId"/> may be empty for a walk-in POS sale with no
/// associated booking. <see cref="Subtotal"/>/<see cref="TaxAmount"/>/
/// <see cref="Total"/> are <see cref="decimal"/>, not the string-money
/// convention every prior module used (e.g. <c>Services.Service.Price</c>) -
/// a deliberate, justified departure: every prior module only ever
/// *displayed* a price, never computed with one, while Accounting's whole
/// job is summing line items, tracking running balances, and computing
/// change - real arithmetic that a formatted string can't safely do.
/// </summary>
public sealed record Invoice(
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
