using AppBookings = Rojan.Desktop.Application.Bookings;
using AppCustomers = Rojan.Desktop.Application.Customers;
using AppInventory = Rojan.Desktop.Application.Inventory;
using AppServices = Rojan.Desktop.Application.Services;
using DomainAccounting = Rojan.Desktop.Domain.Accounting;

namespace Rojan.Desktop.Application.Accounting;

/// <summary>
/// Default <see cref="IInvoiceQueryService"/> implementation - fetches
/// from <see cref="DomainAccounting.IAccountingRepository"/> (Application
/// is allowed to depend on Domain) for invoice data, and composes over
/// Customers/Bookings/Services/Inventory's own query services for
/// <see cref="GetCheckoutOptionsAsync"/> - the same cross-slice
/// orchestration reasoning <c>BookingWorkflow.BookingWorkflowService</c>
/// established in Phase 15: this is normal Clean Architecture use-case
/// composition, not a Domain dependency.
/// </summary>
public sealed class InvoiceQueryService : IInvoiceQueryService
{
    private readonly DomainAccounting.IAccountingRepository _repository;
    private readonly AppCustomers.ICustomerQueryService _customerQueryService;
    private readonly AppBookings.IBookingQueryService _bookingQueryService;
    private readonly AppServices.IServiceQueryService _serviceQueryService;
    private readonly AppInventory.IProductQueryService _productQueryService;

    public InvoiceQueryService(
        DomainAccounting.IAccountingRepository repository,
        AppCustomers.ICustomerQueryService customerQueryService,
        AppBookings.IBookingQueryService bookingQueryService,
        AppServices.IServiceQueryService serviceQueryService,
        AppInventory.IProductQueryService productQueryService)
    {
        _repository = repository;
        _customerQueryService = customerQueryService;
        _bookingQueryService = bookingQueryService;
        _serviceQueryService = serviceQueryService;
        _productQueryService = productQueryService;
    }

    public async Task<IReadOnlyList<InvoiceDto>> GetInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await _repository.GetInvoicesAsync(cancellationToken).ConfigureAwait(true);
        return invoices.Select(AccountingMapper.MapInvoice).ToList();
    }

    /// <summary>
    /// Composes over <see cref="DomainAccounting.IAccountingRepository.GetInvoicesAsync"/>
    /// rather than a dedicated repository search method - same reasoning
    /// as <c>Customers.CustomerQueryService.SearchCustomersAsync</c>.
    /// </summary>
    public async Task<IReadOnlyList<InvoiceDto>> SearchInvoicesAsync(string searchText, CancellationToken cancellationToken = default)
    {
        var invoices = await _repository.GetInvoicesAsync(cancellationToken).ConfigureAwait(true);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return invoices.Select(AccountingMapper.MapInvoice).ToList();
        }

        return invoices
            .Where(invoice =>
                invoice.CustomerName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                invoice.BookingReference.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                invoice.Notes.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .Select(AccountingMapper.MapInvoice)
            .ToList();
    }

    public async Task<InvoiceProfileDto> GetInvoiceProfileAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _repository.GetInvoiceByIdAsync(invoiceId, cancellationToken).ConfigureAwait(true);
        if (invoice is null)
        {
            throw new InvalidOperationException($"Invoice '{invoiceId}' was not found.");
        }

        var items = await _repository.GetInvoiceItemsAsync(invoiceId, cancellationToken).ConfigureAwait(true);
        var payments = await _repository.GetPaymentsForInvoiceAsync(invoiceId, cancellationToken).ConfigureAwait(true);
        var receipts = await _repository.GetReceiptsForInvoiceAsync(invoiceId, cancellationToken).ConfigureAwait(true);

        return new InvoiceProfileDto(
            AccountingMapper.MapInvoice(invoice),
            items.Select(AccountingMapper.MapItem).ToList(),
            payments.Select(AccountingMapper.MapPayment).ToList(),
            receipts.Select(AccountingMapper.MapReceipt).ToList());
    }

    public async Task<CheckoutOptionsDto> GetCheckoutOptionsAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _customerQueryService.GetCustomersAsync(cancellationToken).ConfigureAwait(true);
        var bookings = await _bookingQueryService.GetBookingsAsync(cancellationToken).ConfigureAwait(true);
        var services = await _serviceQueryService.GetServicesAsync(cancellationToken).ConfigureAwait(true);
        var products = await _productQueryService.GetProductsAsync(cancellationToken).ConfigureAwait(true);

        var customerOptions = customers
            .Select(customer => new CheckoutCustomerOptionDto(customer.Id, customer.FullName))
            .ToList();

        var bookingOptions = bookings
            .Where(booking => booking.Status is not (AppBookings.BookingStatus.Cancelled or AppBookings.BookingStatus.NoShow))
            .Select(booking => new CheckoutBookingOptionDto(booking.Id, $"{booking.ServiceName} - {booking.ScheduledAt:MMM d, t}", booking.CustomerId, booking.CustomerName))
            .ToList();

        var serviceOptions = services
            .Where(service => service.Status == AppServices.ServiceStatus.Active)
            .Select(service => new CheckoutServiceOptionDto(service.Id, service.Name, AccountingMapper.ParseMoney(service.Price)))
            .ToList();

        var productOptions = products
            .Where(product => product.Status == AppInventory.ProductStatus.Active)
            .Select(product => new CheckoutProductOptionDto(product.Id, product.Name, AccountingMapper.ParseMoney(product.UnitPrice)))
            .ToList();

        return new CheckoutOptionsDto(customerOptions, bookingOptions, productOptions, serviceOptions);
    }
}
