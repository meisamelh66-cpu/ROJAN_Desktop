namespace Rojan.Desktop.Application.Customers;

/// <summary>Read-only use case Presentation depends on to load Customers - the only way Presentation ever reaches customer data, never through Domain/Infrastructure directly.</summary>
public interface ICustomerQueryService
{
    public Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns customers whose name, company, or email contains <paramref name="searchText"/> (case-insensitive); an empty/whitespace search returns every customer.</summary>
    public Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(string searchText, CancellationToken cancellationToken = default);
}
