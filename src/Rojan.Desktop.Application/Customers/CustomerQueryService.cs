using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Default <see cref="ICustomerQueryService"/> implementation - fetches
/// from <see cref="DomainCustomers.ICustomerRepository"/> (Application is
/// allowed to depend on Domain) and maps every Domain type to its
/// Application-owned equivalent, so nothing Domain-shaped ever crosses into
/// Presentation.
/// </summary>
public sealed class CustomerQueryService : ICustomerQueryService
{
    private readonly DomainCustomers.ICustomerRepository _repository;

    public CustomerQueryService(DomainCustomers.ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _repository.GetCustomersAsync(cancellationToken).ConfigureAwait(true);
        return customers.Select(Map).ToList();
    }

    private static CustomerDto Map(DomainCustomers.Customer customer) => new(
        customer.Id,
        customer.FullName,
        customer.Company,
        customer.Email,
        customer.Phone,
        MapStatus(customer.Status),
        customer.LifetimeValue,
        customer.LastContactedAt,
        customer.Notes);

    private static CustomerStatus MapStatus(DomainCustomers.CustomerStatus status) => status switch
    {
        DomainCustomers.CustomerStatus.Lead => CustomerStatus.Lead,
        DomainCustomers.CustomerStatus.Active => CustomerStatus.Active,
        DomainCustomers.CustomerStatus.Inactive => CustomerStatus.Inactive,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown domain customer status."),
    };
}
