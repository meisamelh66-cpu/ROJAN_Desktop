using Rojan.Desktop.Application.Bookings;
using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Application.Inventory;
using Rojan.Desktop.Application.Search;
using Rojan.Desktop.Application.Specialists;
using AppServices = Rojan.Desktop.Application.Services;

namespace Rojan.Desktop.Application.Tests.Search;

/// <summary>Fixed-list query-service test doubles feeding <see cref="GlobalSearchIndexServiceTests"/> - one seed row per module, just enough to verify <see cref="GlobalSearchIndexService"/>'s mapping shape.</summary>
internal sealed class StubCustomerQueryServiceForSearch : ICustomerQueryService
{
    public Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CustomerDto>>([
            new CustomerDto("c1", "Sarah Johnson", "Acme Spa", "sarah@acme.com", "555-0100", CustomerStatus.Active, "1200", DateTimeOffset.UtcNow, "", "org1", "branch1"),
        ]);

    public Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(string searchText, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by GlobalSearchIndexService.");

    public Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(CustomerSearchFilter filter, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by GlobalSearchIndexService.");
}

internal sealed class StubBookingQueryServiceForSearch : IBookingQueryService
{
    public Task<IReadOnlyList<BookingDto>> GetBookingsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BookingDto>>([
            new BookingDto("b1", "c1", "Sarah Johnson", "s1", "Haircut", "sp1", "Alex Stylist", DateTimeOffset.UtcNow, 60, "80", BookingStatus.Confirmed, "", "org1", "branch1"),
        ]);

    public Task<BookingDto?> GetBookingByIdAsync(string bookingId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by GlobalSearchIndexService.");

    public Task<IReadOnlyList<BookingDto>> SearchBookingsAsync(BookingSearchFilter filter, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by GlobalSearchIndexService.");
}

internal sealed class StubSpecialistQueryServiceForSearch : ISpecialistQueryService
{
    public Task<IReadOnlyList<SpecialistDto>> GetSpecialistsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SpecialistDto>>([
            new SpecialistDto("sp1", "Alex Stylist", "Senior Stylist", "alex@rojan.com", "555-0200", SpecialistStatus.Active, ""),
        ]);

    public Task<IReadOnlyList<SpecialistDto>> SearchSpecialistsAsync(string searchText, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by GlobalSearchIndexService.");

    public Task<IReadOnlyList<SpecialistDto>> SearchSpecialistsAsync(SpecialistSearchFilter filter, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by GlobalSearchIndexService.");

    public Task<IReadOnlyList<string>> GetAssignedServiceIdsAsync(string specialistId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by GlobalSearchIndexService.");
}

internal sealed class StubServiceQueryServiceForSearch : AppServices.IServiceQueryService
{
    public Task<IReadOnlyList<AppServices.ServiceDto>> GetServicesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AppServices.ServiceDto>>([
            new AppServices.ServiceDto("s1", "Haircut", AppServices.ServiceCategory.Hair, AppServices.ServiceStatus.Active, 60, "80", ""),
        ]);

    public Task<IReadOnlyList<AppServices.ServiceDto>> SearchServicesAsync(string searchText, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by GlobalSearchIndexService.");

    public Task<IReadOnlyList<AppServices.ServiceDto>> SearchServicesAsync(AppServices.ServiceSearchFilter filter, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by GlobalSearchIndexService.");
}

internal sealed class StubProductQueryServiceForSearch : IProductQueryService
{
    public Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProductDto>>([
            new ProductDto("p1", "SKU-100", "Shampoo", "cat1", "Hair Care", "sup1", "Beauty Supply Co", "15", ProductStatus.Active, "", "org1", "branch1"),
        ]);

    public Task<IReadOnlyList<ProductDto>> SearchProductsAsync(string searchText, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not used by GlobalSearchIndexService.");

    public Task<IReadOnlyList<ProductCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProductCategoryDto>>([]);

    public Task<IReadOnlyList<SupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SupplierDto>>([]);
}
