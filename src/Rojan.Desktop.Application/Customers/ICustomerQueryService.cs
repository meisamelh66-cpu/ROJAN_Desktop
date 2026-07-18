namespace Rojan.Desktop.Application.Customers;

/// <summary>Read-only use case Presentation depends on to load Customers - the only way Presentation ever reaches customer data, never through Domain/Infrastructure directly.</summary>
public interface ICustomerQueryService
{
    public Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default);
}
