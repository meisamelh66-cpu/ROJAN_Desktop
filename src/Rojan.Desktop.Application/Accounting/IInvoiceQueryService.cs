namespace Rojan.Desktop.Application.Accounting;

/// <summary>Read-only use cases Presentation depends on to load Invoices - the only way Presentation ever reaches invoice data, never through Domain/Infrastructure directly.</summary>
public interface IInvoiceQueryService
{
    public Task<IReadOnlyList<InvoiceDto>> GetInvoicesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns invoices whose customer name, booking reference, or notes contain <paramref name="searchText"/> (case-insensitive); an empty/whitespace search returns every invoice.</summary>
    public Task<IReadOnlyList<InvoiceDto>> SearchInvoicesAsync(string searchText, CancellationToken cancellationToken = default);

    public Task<InvoiceProfileDto> GetInvoiceProfileAsync(string invoiceId, CancellationToken cancellationToken = default);

    /// <summary>Everything the POS checkout's cart step needs - Customers, open Bookings, Active-only Products, and Active-only Services - composing over Customers/Bookings/Services/Inventory's own query services ("Integrate with Booking, Customer, Inventory").</summary>
    public Task<CheckoutOptionsDto> GetCheckoutOptionsAsync(CancellationToken cancellationToken = default);
}
