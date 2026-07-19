namespace Rojan.Desktop.Presentation.ViewModels.Accounting;

/// <summary>
/// One line in the POS checkout's cart - a product or service picked from
/// <c>CheckoutOptionsDto</c> plus a quantity. Immutable once added (no
/// in-cart quantity editing - remove and re-add instead); a foundation-scope
/// simplification, same reasoning documented on <c>PosCheckoutViewModel</c>.
/// </summary>
public sealed class PosCartLine
{
    public required string ProductId { get; init; }

    public required string ServiceId { get; init; }

    public required string Description { get; init; }

    public required int Quantity { get; init; }

    public required decimal UnitPrice { get; init; }

    public decimal LineTotal => Quantity * UnitPrice;
}
