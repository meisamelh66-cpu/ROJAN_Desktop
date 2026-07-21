using Rojan.Desktop.Application.Bookings;
using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Application.Inventory;
using Rojan.Desktop.Application.Specialists;
using AppServices = Rojan.Desktop.Application.Services;

namespace Rojan.Desktop.Application.Search;

/// <summary>
/// Default <see cref="IGlobalSearchIndexService"/>. Queries every
/// relevant module's existing <c>*QueryService</c> in parallel (all are
/// Application-layer siblings - depending on them is depending on the
/// same layer, not a Clean Architecture violation, the same reasoning
/// <c>Reporting</c>/<c>Analytics</c>'s existing cross-module aggregation
/// services already establish) and maps each result into a
/// <see cref="SearchCandidate"/>. No caching - each call re-queries live
/// data, since a global search must never show stale customer/booking
/// state; the underlying query services are themselves in-memory/fast
/// today, so this is cheap. A future phase indexing a real database
/// would cache/debounce here without changing this interface.
/// </summary>
public sealed class GlobalSearchIndexService : IGlobalSearchIndexService
{
    private readonly ICustomerQueryService _customerQueryService;
    private readonly IBookingQueryService _bookingQueryService;
    private readonly ISpecialistQueryService _specialistQueryService;
    private readonly AppServices.IServiceQueryService _serviceQueryService;
    private readonly IProductQueryService _productQueryService;

    public GlobalSearchIndexService(
        ICustomerQueryService customerQueryService,
        IBookingQueryService bookingQueryService,
        ISpecialistQueryService specialistQueryService,
        AppServices.IServiceQueryService serviceQueryService,
        IProductQueryService productQueryService)
    {
        _customerQueryService = customerQueryService;
        _bookingQueryService = bookingQueryService;
        _specialistQueryService = specialistQueryService;
        _serviceQueryService = serviceQueryService;
        _productQueryService = productQueryService;
    }

    public async Task<IReadOnlyList<SearchCandidate>> GetCandidatesAsync(CancellationToken cancellationToken = default)
    {
        var customersTask = _customerQueryService.GetCustomersAsync(cancellationToken);
        var bookingsTask = _bookingQueryService.GetBookingsAsync(cancellationToken);
        var specialistsTask = _specialistQueryService.GetSpecialistsAsync(cancellationToken);
        var servicesTask = _serviceQueryService.GetServicesAsync(cancellationToken);
        var productsTask = _productQueryService.GetProductsAsync(cancellationToken);

        await Task.WhenAll(customersTask, bookingsTask, specialistsTask, servicesTask, productsTask).ConfigureAwait(false);

        var candidates = new List<SearchCandidate>();

        foreach (var customer in customersTask.Result)
        {
            candidates.Add(new SearchCandidate(
                $"customer:{customer.Id}",
                SearchResultType.Customer,
                customer.FullName,
                customer.Company,
                [customer.Email, customer.Phone],
                "page:customers"));
        }

        foreach (var booking in bookingsTask.Result)
        {
            candidates.Add(new SearchCandidate(
                $"booking:{booking.Id}",
                SearchResultType.Booking,
                $"{booking.CustomerName} — {booking.ServiceName}",
                booking.SpecialistName,
                [booking.CustomerName, booking.ServiceName, booking.SpecialistName],
                "page:bookings"));
        }

        foreach (var specialist in specialistsTask.Result)
        {
            candidates.Add(new SearchCandidate(
                $"specialist:{specialist.Id}",
                SearchResultType.Specialist,
                specialist.FullName,
                specialist.Title,
                [specialist.Email],
                "page:specialists"));
        }

        foreach (var service in servicesTask.Result)
        {
            candidates.Add(new SearchCandidate(
                $"service:{service.Id}",
                SearchResultType.Service,
                service.Name,
                service.Price,
                [service.Category.ToString()],
                "page:services"));
        }

        foreach (var product in productsTask.Result)
        {
            candidates.Add(new SearchCandidate(
                $"product:{product.Id}",
                SearchResultType.Product,
                product.Name,
                product.Sku,
                [product.Sku, product.CategoryName, product.SupplierName],
                "page:inventory"));
        }

        return candidates;
    }
}
