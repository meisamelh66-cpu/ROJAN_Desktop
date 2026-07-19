using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Default <see cref="ICustomerQueryService"/> implementation - fetches
/// from <see cref="DomainCustomers.ICustomerRepository"/> (Application is
/// allowed to depend on Domain) and maps every Domain type to its
/// Application-owned equivalent via <see cref="CustomerMapper"/>, so
/// nothing Domain-shaped ever crosses into Presentation.
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
        return customers.Select(CustomerMapper.MapCustomer).ToList();
    }

    /// <summary>
    /// Composes over <see cref="DomainCustomers.ICustomerRepository.GetCustomersAsync"/>
    /// rather than a dedicated repository search method - search is a
    /// read-composition concern Application owns, not a new Domain
    /// contract, keeping the repository's own surface minimal (same
    /// reasoning that keeps Domain a thin data+contract layer everywhere
    /// else in this vertical slice).
    /// </summary>
    public async Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(string searchText, CancellationToken cancellationToken = default)
    {
        var customers = await _repository.GetCustomersAsync(cancellationToken).ConfigureAwait(true);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return customers.Select(CustomerMapper.MapCustomer).ToList();
        }

        return customers
            .Where(customer =>
                customer.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                customer.Company.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                customer.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .Select(CustomerMapper.MapCustomer)
            .ToList();
    }
}
