namespace Rojan.Server.Domain.Customers;

/// <summary>
/// Persistence contract for <see cref="Customer"/>.
/// <see cref="GetByIdAsync"/> takes <c>organizationId</c> as a
/// required parameter, not just <c>customerId</c> - tenant isolation is
/// enforced here, at the repository/query level, not left to
/// <c>Application.Customers.CustomerService</c> to remember to filter
/// results after the fact. A lookup for a customer that exists but
/// belongs to a different organization returns <see langword="null"/>,
/// exactly the same as a lookup for a customer that does not exist at
/// all - the caller cannot distinguish "wrong tenant" from "never
/// existed," which is the point (an API response must never confirm
/// another tenant's data exists).
/// </summary>
public interface ICustomerRepository
{
    public Task<Customer> CreateAsync(Customer customer, CancellationToken cancellationToken = default);

    public Task<Customer?> GetByIdAsync(string organizationId, string customerId, CancellationToken cancellationToken = default);

    /// <summary>Every customer within one tenant, never across organizations.</summary>
    public Task<IReadOnlyList<Customer>> GetByOrganizationIdAsync(string organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists an update to an already-loaded <see cref="Customer"/>.
    /// Does not itself re-check <see cref="Customer.OrganizationId"/> -
    /// callers must only ever pass a record obtained from
    /// <see cref="GetByIdAsync"/> (already tenant-scoped) with its
    /// <see cref="Customer.OrganizationId"/> preserved unchanged, never
    /// one built from caller-supplied/request-body data (see
    /// <c>Application.Customers.CustomerService.UpdateCustomerAsync</c>'s
    /// own doc comment).
    /// </summary>
    public Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
}
