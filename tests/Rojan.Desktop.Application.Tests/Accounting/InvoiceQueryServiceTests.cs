using Rojan.Desktop.Application.Accounting;
using AppBookings = Rojan.Desktop.Application.Bookings;
using AppCustomers = Rojan.Desktop.Application.Customers;
using AppInventory = Rojan.Desktop.Application.Inventory;
using AppServices = Rojan.Desktop.Application.Services;
using DomainAccounting = Rojan.Desktop.Domain.Accounting;

namespace Rojan.Desktop.Application.Tests.Accounting;

public sealed class InvoiceQueryServiceTests
{
    private static DomainAccounting.Invoice MakeInvoice(string id, string customerName, string notes = "") =>
        new(id, "customer-1", customerName, "booking-1", "Manicure - Amelia Hart", DateTimeOffset.UnixEpoch, DomainAccounting.InvoiceStatus.Issued, 40m, 3.20m, 43.20m, notes);

    private static InvoiceQueryService MakeSut(
        StubAccountingRepository? repository = null,
        StubCustomerQueryService? customerQueryService = null,
        StubBookingQueryService? bookingQueryService = null,
        StubServiceQueryService? serviceQueryService = null,
        StubProductQueryService? productQueryService = null) => new(
        repository ?? new StubAccountingRepository(),
        customerQueryService ?? new StubCustomerQueryService([]),
        bookingQueryService ?? new StubBookingQueryService([]),
        serviceQueryService ?? new StubServiceQueryService([]),
        productQueryService ?? new StubProductQueryService([]));

    [Fact]
    public async Task GetInvoicesAsync_ReturnsMappedInvoices()
    {
        var repository = new StubAccountingRepository();
        repository.Invoices.Add(MakeInvoice("invoice-1", "Amelia Hart"));
        var sut = MakeSut(repository);

        var result = await sut.GetInvoicesAsync();

        var invoice = Assert.Single(result);
        Assert.Equal("Amelia Hart", invoice.CustomerName);
    }

    [Fact]
    public async Task SearchInvoicesAsync_MatchesCustomerName_ReturnsOnlyMatchingInvoices()
    {
        var repository = new StubAccountingRepository();
        repository.Invoices.Add(MakeInvoice("invoice-1", "Amelia Hart"));
        repository.Invoices.Add(MakeInvoice("invoice-2", "Sophia Reyes"));
        var sut = MakeSut(repository);

        var result = await sut.SearchInvoicesAsync("sophia");

        var invoice = Assert.Single(result);
        Assert.Equal("invoice-2", invoice.Id);
    }

    [Fact]
    public async Task SearchInvoicesAsync_EmptySearchText_ReturnsEveryInvoice()
    {
        var repository = new StubAccountingRepository();
        repository.Invoices.Add(MakeInvoice("invoice-1", "Amelia Hart"));
        repository.Invoices.Add(MakeInvoice("invoice-2", "Sophia Reyes"));
        var sut = MakeSut(repository);

        var result = await sut.SearchInvoicesAsync(string.Empty);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetInvoiceProfileAsync_KnownInvoice_ReturnsInvoiceItemsPaymentsAndReceipts()
    {
        var repository = new StubAccountingRepository();
        var invoice = MakeInvoice("invoice-1", "Amelia Hart");
        repository.Invoices.Add(invoice);
        repository.Items.Add(new DomainAccounting.InvoiceItem("line-1", "invoice-1", string.Empty, "service-4", "Manicure", 1, 40m, 40m));
        repository.Payments.Add(new DomainAccounting.Payment("payment-1", "invoice-1", "customer-1", "Amelia Hart", DomainAccounting.PaymentMethod.Cash, 43.20m, DateTimeOffset.UnixEpoch, string.Empty, string.Empty));
        repository.Receipts.Add(new DomainAccounting.Receipt("receipt-1", "payment-1", "invoice-1", DateTimeOffset.UnixEpoch, 43.20m, "Amelia Hart"));
        var sut = MakeSut(repository);

        var profile = await sut.GetInvoiceProfileAsync("invoice-1");

        Assert.Equal("Amelia Hart", profile.Invoice.CustomerName);
        Assert.Single(profile.Items);
        Assert.Single(profile.Payments);
        Assert.Single(profile.Receipts);
    }

    [Fact]
    public async Task GetInvoiceProfileAsync_UnknownInvoice_ThrowsInvalidOperationException()
    {
        var sut = MakeSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetInvoiceProfileAsync("no-such-invoice"));
    }

    [Fact]
    public async Task GetCheckoutOptionsAsync_FiltersBookingsServicesAndProductsToActiveOnly()
    {
        var customerQueryService = new StubCustomerQueryService([new AppCustomers.CustomerDto("customer-1", "Amelia Hart", string.Empty, string.Empty, string.Empty, AppCustomers.CustomerStatus.Active, "$0", DateTimeOffset.UnixEpoch, string.Empty, "org-1", "branch-1")]);
        var bookingQueryService = new StubBookingQueryService([
            new AppBookings.BookingDto("booking-1", "customer-1", "Amelia Hart", "service-1", "Haircut & Style", "specialist-1", "Jordan Lee", DateTimeOffset.UnixEpoch, 60, "$65", AppBookings.BookingStatus.Confirmed, string.Empty, "org-1", "branch-1"),
            new AppBookings.BookingDto("booking-2", "customer-1", "Amelia Hart", "service-1", "Haircut & Style", "specialist-1", "Jordan Lee", DateTimeOffset.UnixEpoch, 60, "$65", AppBookings.BookingStatus.Cancelled, string.Empty, "org-1", "branch-1"),
        ]);
        var serviceQueryService = new StubServiceQueryService([
            new AppServices.ServiceDto("service-1", "Haircut & Style", AppServices.ServiceCategory.Hair, AppServices.ServiceStatus.Active, 60, "$65", string.Empty),
            new AppServices.ServiceDto("service-9", "Perm Styling", AppServices.ServiceCategory.Hair, AppServices.ServiceStatus.Discontinued, 90, "$70", string.Empty),
        ]);
        var productQueryService = new StubProductQueryService([
            new AppInventory.ProductDto("product-1", "SKU-1", "Hydrating Shampoo 1L", "category-1", "Hair Care", "supplier-1", "Glow", "$18", AppInventory.ProductStatus.Active, string.Empty, "org-1", "branch-1"),
            new AppInventory.ProductDto("product-10", "SKU-10", "Colour Brush Set", "category-6", "Tools", "supplier-3", "Luxe", "$34", AppInventory.ProductStatus.Discontinued, string.Empty, "org-1", "branch-1"),
        ]);
        var sut = MakeSut(customerQueryService: customerQueryService, bookingQueryService: bookingQueryService, serviceQueryService: serviceQueryService, productQueryService: productQueryService);

        var options = await sut.GetCheckoutOptionsAsync();

        Assert.Single(options.Customers);
        Assert.Single(options.Bookings);
        Assert.Equal("booking-1", options.Bookings[0].Id);
        Assert.Single(options.Services);
        Assert.Equal(65m, options.Services[0].Price);
        Assert.Single(options.Products);
        Assert.Equal(18m, options.Products[0].UnitPrice);
    }
}
