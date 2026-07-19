namespace Rojan.Desktop.Application.Accounting;

/// <summary>A pickable customer for the POS checkout's cart step - see <see cref="IInvoiceQueryService.GetCheckoutOptionsAsync"/>.</summary>
public sealed record CheckoutCustomerOptionDto(string Id, string FullName);

/// <summary>A pickable open booking to bill against - "Integrate with Booking": lets a checkout be linked to an existing booking rather than always being a standalone walk-in sale.</summary>
public sealed record CheckoutBookingOptionDto(string Id, string Reference, string CustomerId, string CustomerName);

/// <summary>A pickable, Active-only catalog product for the POS checkout's cart step - "Integrate with Inventory".</summary>
public sealed record CheckoutProductOptionDto(string Id, string Name, decimal UnitPrice);

/// <summary>A pickable, Active-only catalog service for the POS checkout's cart step.</summary>
public sealed record CheckoutServiceOptionDto(string Id, string Name, decimal Price);

/// <summary>Everything the POS checkout's cart step needs, fetched together as a single unit of work - the "booking options query" precedent (<c>BookingWorkflow.BookingOptionsDto</c>) applied to checkout.</summary>
public sealed record CheckoutOptionsDto(
    IReadOnlyList<CheckoutCustomerOptionDto> Customers,
    IReadOnlyList<CheckoutBookingOptionDto> Bookings,
    IReadOnlyList<CheckoutProductOptionDto> Products,
    IReadOnlyList<CheckoutServiceOptionDto> Services);
