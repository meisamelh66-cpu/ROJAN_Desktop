namespace Rojan.Desktop.Application.Customers;

/// <summary>Read-only use case Presentation depends on to load Customers - the only way Presentation ever reaches customer data, never through Domain/Infrastructure directly.</summary>
public interface ICustomerQueryService
{
    public Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns customers whose name, company, or email contains <paramref name="searchText"/> (case-insensitive); an empty/whitespace search returns every customer. Predates the <see cref="CustomerSearchFilter"/> overload below (Sprint 4 Commit 2 added that one alongside this one rather than replacing it, to avoid touching every existing caller/test double in one pass) - <c>Search.GlobalSearchIndexService</c>, the one module that might look like a natural consumer, actually calls <see cref="GetCustomersAsync"/> directly and does its own text ranking downstream via <c>SearchRankingService</c>, so this overload currently has no production caller of its own; kept as part of the public interface surface rather than removed, since deleting it would ripple into every test double implementing this interface for no Sprint 4 reason.</summary>
    public Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(string searchText, CancellationToken cancellationToken = default);

    /// <summary>Returns customers matching every non-null/non-empty criterion in <paramref name="filter"/> (ANDed) - an all-default <see cref="CustomerSearchFilter"/> returns every customer, identical to <see cref="GetCustomersAsync"/>.</summary>
    public Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(CustomerSearchFilter filter, CancellationToken cancellationToken = default);
}
