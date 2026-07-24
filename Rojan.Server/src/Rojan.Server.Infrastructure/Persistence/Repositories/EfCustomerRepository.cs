using Microsoft.EntityFrameworkCore;
using Rojan.Server.Domain.Customers;

namespace Rojan.Server.Infrastructure.Persistence.Repositories;

/// <summary>
/// Default <see cref="ICustomerRepository"/> - same "inject the scoped
/// DbContext directly" reasoning as
/// <c>Repositories.EfOrganizationRepository</c>'s own doc comment.
/// <see cref="GetByIdAsync"/>/<see cref="GetByOrganizationIdAsync"/> both
/// filter on <c>OrganizationId</c> directly in the query - the actual
/// enforcement point for "a query from one organization must never return
/// another organization's data" (the index on that column, added by
/// <c>Configurations.CustomerConfiguration</c>, only keeps that filter
/// cheap; it is not what makes it correct).
/// </summary>
public sealed class EfCustomerRepository : ICustomerRepository
{
    private readonly RojanServerDbContext _dbContext;

    public EfCustomerRepository(RojanServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Customer> CreateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return customer;
    }

    public Task<Customer?> GetByIdAsync(string organizationId, string customerId, CancellationToken cancellationToken = default) =>
        _dbContext.Customers.FirstOrDefaultAsync(
            customer => customer.Id == customerId && customer.OrganizationId == organizationId,
            cancellationToken);

    public async Task<IReadOnlyList<Customer>> GetByOrganizationIdAsync(string organizationId, CancellationToken cancellationToken = default) =>
        await _dbContext.Customers
            .Where(customer => customer.OrganizationId == organizationId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Customers
            .FirstOrDefaultAsync(
                tracked => tracked.Id == customer.Id && tracked.OrganizationId == customer.OrganizationId,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            // The caller (Application.Customers.CustomerService) always
            // loads via GetByIdAsync first, which is itself tenant-scoped -
            // reaching here with no matching row means the organization id
            // was tampered with between load and save, which must never
            // silently succeed as an update to the wrong tenant's data.
            throw new InvalidOperationException($"Customer '{customer.Id}' was not found for organization '{customer.OrganizationId}'.");
        }

        _dbContext.Entry(existing).CurrentValues.SetValues(customer);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return customer;
    }
}
