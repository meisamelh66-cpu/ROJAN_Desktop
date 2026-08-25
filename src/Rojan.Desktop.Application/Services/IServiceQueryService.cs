namespace Rojan.Desktop.Application.Services;

/// <summary>Read-only use case Presentation depends on to load the service catalog - the only way Presentation ever reaches service data, never through Domain/Infrastructure directly.</summary>
public interface IServiceQueryService
{
    public Task<IReadOnlyList<ServiceDto>> GetServicesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns services whose name, category, or description contains <paramref name="searchText"/> (case-insensitive); an empty/whitespace search returns every service. Predates the <see cref="ServiceSearchFilter"/> overload below (Sprint 5 Commit 2 added that one alongside this one rather than replacing it, same reasoning <c>Customers.ICustomerQueryService.SearchCustomersAsync(string, CancellationToken)</c>'s own doc comment documents) - kept as part of the public interface surface for its existing test-double coverage, even though <c>ServicePageViewModel</c> now calls the filter-based overload exclusively.</summary>
    public Task<IReadOnlyList<ServiceDto>> SearchServicesAsync(string searchText, CancellationToken cancellationToken = default);

    /// <summary>Returns services matching every non-null/non-empty criterion in <paramref name="filter"/> (ANDed) - an all-default <see cref="ServiceSearchFilter"/> returns every service, identical to <see cref="GetServicesAsync"/>.</summary>
    public Task<IReadOnlyList<ServiceDto>> SearchServicesAsync(ServiceSearchFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Service Catalog Management: every real category for the current salon, needed to populate a Create-Service category picker - a read, not a command, same reasoning every other read-only lookup in this app lives on the query service.</summary>
    public Task<IReadOnlyList<ServiceCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
