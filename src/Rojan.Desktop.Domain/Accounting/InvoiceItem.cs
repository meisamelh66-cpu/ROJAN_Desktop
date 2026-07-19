namespace Rojan.Desktop.Domain.Accounting;

/// <summary>
/// A single line item on an <see cref="Invoice"/>. <see cref="ProductId"/>
/// (may reference <c>Domain.Inventory.Product</c>) and <see cref="ServiceId"/>
/// (may reference <c>Domain.Services.Service</c>) are free-form,
/// unvalidated cross-slice references, same reasoning as
/// <c>Invoice.CustomerId</c> - exactly one of the two is normally
/// populated (a retail product line vs. a billed service line), never
/// both; both may be empty for a fully custom line (<see cref="Description"/>
/// alone).
/// </summary>
public sealed record InvoiceItem(
    string Id,
    string InvoiceId,
    string ProductId,
    string ServiceId,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
