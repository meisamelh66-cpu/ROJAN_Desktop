using Rojan.Desktop.Application.Organizations;
using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Default <see cref="ICustomerQueryService"/> implementation - fetches
/// from <see cref="DomainCustomers.ICustomerRepository"/> (Application is
/// allowed to depend on Domain) and maps every Domain type to its
/// Application-owned equivalent via <see cref="CustomerMapper"/>, so
/// nothing Domain-shaped ever crosses into Presentation.
///
/// Phase 22A: every read is additionally scoped to
/// <see cref="IEnterpriseContext.CurrentOrganizationId"/>/
/// <see cref="IEnterpriseContext.CurrentBranchId"/> - the concrete
/// "no cross-organization/cross-branch data access" guarantee for the
/// Customers module (also named "CRM" in this phase's module list).
/// Filtering happens here, the one boundary every Presentation caller
/// actually goes through, rather than in <see cref="DomainCustomers.ICustomerRepository"/>
/// itself, so the repository contract stays unchanged for any other
/// consumer.
/// </summary>
public sealed class CustomerQueryService : ICustomerQueryService
{
    private readonly DomainCustomers.ICustomerRepository _repository;
    private readonly IEnterpriseContext _enterpriseContext;

    public CustomerQueryService(DomainCustomers.ICustomerRepository repository, IEnterpriseContext enterpriseContext)
    {
        _repository = repository;
        _enterpriseContext = enterpriseContext;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _repository.GetCustomersAsync(cancellationToken).ConfigureAwait(true);
        return ScopeToCurrentSession(customers).Select(CustomerMapper.MapCustomer).ToList();
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
        var customers = ScopeToCurrentSession(await _repository.GetCustomersAsync(cancellationToken).ConfigureAwait(true));

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

    /// <summary>Organization/Branch Scoping - matches by organization always, and by branch only when a branch is selected (a Platform/Organization-level session with no branch chosen yet sees every branch within its organization, never another organization's).</summary>
    private IEnumerable<DomainCustomers.Customer> ScopeToCurrentSession(IEnumerable<DomainCustomers.Customer> customers) =>
        customers.Where(customer =>
            customer.OrganizationId == _enterpriseContext.CurrentOrganizationId &&
            (_enterpriseContext.CurrentBranchId is null || customer.BranchId == _enterpriseContext.CurrentBranchId));
}
