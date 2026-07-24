using Rojan.Server.Application.Tenancy;
using Rojan.Server.Domain.Authentication;
using Rojan.Server.Domain.Customers;

namespace Rojan.Server.Application.Customers;

/// <summary>
/// Default <see cref="ICustomerService"/>. Every method reads
/// <see cref="ITenantContext.OrganizationId"/> itself rather than
/// accepting it as a parameter - the one and only source of tenant
/// identity, never request-body/client-supplied data (see this class's
/// own interface doc comment). <see cref="UpdateCustomerAsync"/> always
/// loads the existing record first (via
/// <see cref="ICustomerRepository.GetByIdAsync"/>, itself tenant-scoped)
/// and preserves its <see cref="Customer.OrganizationId"/> unchanged when
/// building the updated record - the same "editing must never silently
/// move a record to a different tenant/branch" reasoning the desktop
/// solution's own <c>Application.Customers.CustomerCommandService.UpdateCustomerAsync</c>
/// already establishes for organization/branch stamping.
/// </summary>
public sealed class CustomerService : ICustomerService
{
    private readonly ITenantContext _tenantContext;
    private readonly ICustomerRepository _customerRepository;
    private readonly IBranchRepository _branchRepository;

    public CustomerService(ITenantContext tenantContext, ICustomerRepository customerRepository, IBranchRepository branchRepository)
    {
        _tenantContext = tenantContext;
        _customerRepository = customerRepository;
        _branchRepository = branchRepository;
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureValidBranchAsync(request.BranchId, cancellationToken).ConfigureAwait(true);

        var now = DateTimeOffset.UtcNow;
        var customer = new Customer(
            Guid.NewGuid().ToString(),
            _tenantContext.OrganizationId,
            request.BranchId,
            request.Name,
            request.Phone,
            request.Email,
            CustomerStatus.Active,
            now,
            now);

        var created = await _customerRepository.CreateAsync(customer, cancellationToken).ConfigureAwait(true);

        return Map(created);
    }

    public async Task<CustomerDto> GetCustomerAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(_tenantContext.OrganizationId, customerId, cancellationToken).ConfigureAwait(true)
            ?? throw new CustomerNotFoundException(customerId);

        return Map(customer);
    }

    public async Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.GetByOrganizationIdAsync(_tenantContext.OrganizationId, cancellationToken).ConfigureAwait(true);

        return customers.Select(Map).ToList();
    }

    public async Task<CustomerDto> UpdateCustomerAsync(string customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _customerRepository.GetByIdAsync(_tenantContext.OrganizationId, customerId, cancellationToken).ConfigureAwait(true)
            ?? throw new CustomerNotFoundException(customerId);

        if (!Enum.TryParse<CustomerStatus>(request.Status, ignoreCase: true, out var newStatus))
        {
            throw new InvalidCustomerStatusException($"'{request.Status}' is not a recognized customer status.");
        }

        if (newStatus != existing.Status && !CustomerRules.IsValidTransition(existing.Status, newStatus))
        {
            throw new InvalidCustomerStatusException($"Cannot transition customer from {existing.Status} to {newStatus}.");
        }

        await EnsureValidBranchAsync(request.BranchId, cancellationToken).ConfigureAwait(true);

        var updated = existing with
        {
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            BranchId = request.BranchId,
            Status = newStatus,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var saved = await _customerRepository.UpdateAsync(updated, cancellationToken).ConfigureAwait(true);

        return Map(saved);
    }

    private async Task EnsureValidBranchAsync(string? branchId, CancellationToken cancellationToken)
    {
        if (branchId is null)
        {
            return;
        }

        var branch = await _branchRepository.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(true);
        if (branch is null || !UserRules.IsValidBranchAssignment(_tenantContext.OrganizationId, branch))
        {
            throw new InvalidCustomerBranchException(branchId);
        }
    }

    private static CustomerDto Map(Customer customer) =>
        new(customer.Id, customer.BranchId, customer.Name, customer.Phone, customer.Email, customer.Status.ToString(), customer.CreatedAt, customer.UpdatedAt);
}
