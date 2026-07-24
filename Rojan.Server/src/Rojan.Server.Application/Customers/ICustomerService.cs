namespace Rojan.Server.Application.Customers;

/// <summary>
/// Sprint 8 Commit 4: Tenant-Aware Customer API. Every operation is
/// scoped to <c>Application.Tenancy.ITenantContext.OrganizationId</c> -
/// none of them accept an organization id as a parameter or read one from
/// a request DTO (see <see cref="CreateCustomerRequest"/>/
/// <see cref="UpdateCustomerRequest"/>'s own doc comments); the tenant
/// always comes from the authenticated session, never from
/// caller-supplied data.
/// </summary>
public interface ICustomerService
{
    public Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>Throws <see cref="CustomerNotFoundException"/> if <paramref name="customerId"/> does not exist within the caller's own organization.</summary>
    public Task<CustomerDto> GetCustomerAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>Every customer within the caller's own organization - never another tenant's.</summary>
    public Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default);

    /// <summary>Throws <see cref="CustomerNotFoundException"/> if <paramref name="customerId"/> does not exist within the caller's own organization.</summary>
    public Task<CustomerDto> UpdateCustomerAsync(string customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
}
