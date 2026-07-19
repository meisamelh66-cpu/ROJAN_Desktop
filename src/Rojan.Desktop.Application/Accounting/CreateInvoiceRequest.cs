namespace Rojan.Desktop.Application.Accounting;

/// <summary>One cart line for <see cref="IInvoiceCommandService.CreateInvoiceAsync"/> - exactly one of <see cref="ProductId"/>/<see cref="ServiceId"/> is normally populated, matching <c>Domain.Accounting.InvoiceItem</c>'s own convention.</summary>
public sealed record CreateInvoiceItemRequest(string ProductId, string ServiceId, string Description, int Quantity, decimal UnitPrice);

/// <summary>
/// Input to <see cref="IInvoiceCommandService.CreateInvoiceAsync"/> - the
/// POS checkout's whole cart. Customer/Booking id and name travel
/// together (Presentation resolves both from the selected dropdown item),
/// same reasoning as <c>BookingWorkflow.CreateBookingWorkflowRequest</c>.
/// New invoices always start as <c>Issued</c>, so Status isn't a
/// caller-supplied field.
/// </summary>
public sealed record CreateInvoiceRequest(
    string CustomerId,
    string CustomerName,
    string BookingId,
    string BookingReference,
    IReadOnlyList<CreateInvoiceItemRequest> Items,
    decimal TaxRate,
    string Notes);
