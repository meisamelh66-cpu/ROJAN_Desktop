namespace Rojan.Desktop.Application.Customers;

/// <summary>Read-only use case Presentation depends on to load Customers - the only way Presentation ever reaches customer data, never through Domain/Infrastructure directly.</summary>
public interface ICustomerQueryService
{
    public Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns customers whose name, company, or email contains <paramref name="searchText"/> (case-insensitive); an empty/whitespace search returns every customer. Kept alongside the <see cref="CustomerSearchFilter"/> overload below for existing consumers (e.g. the global command-palette search) that only ever need a single free-text query.</summary>
    public Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(string searchText, CancellationToken cancellationToken = default);

    /// <summary>Returns customers matching every non-null/non-empty criterion in <paramref name="filter"/> (ANDed) - an all-default <see cref="CustomerSearchFilter"/> returns every customer, identical to <see cref="GetCustomersAsync"/>.</summary>
    public Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(CustomerSearchFilter filter, CancellationToken cancellationToken = default);
}
