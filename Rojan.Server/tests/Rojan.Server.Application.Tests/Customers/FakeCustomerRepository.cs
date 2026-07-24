using Rojan.Server.Domain.Customers;

namespace Rojan.Server.Application.Tests.Customers;

/// <summary>In-memory <see cref="ICustomerRepository"/> test double - filters exactly the same way <c>Infrastructure.Persistence.Repositories.EfCustomerRepository</c> does, so tenant-isolation behavior is exercised the same way here as it would be against a real database.</summary>
internal sealed class FakeCustomerRepository : ICustomerRepository
{
    public List<Customer> Customers { get; } = [];

    public Task<Customer> CreateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        Customers.Add(customer);
        return Task.FromResult(customer);
    }

    public Task<Customer?> GetByIdAsync(string organizationId, string customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Customers.FirstOrDefault(customer => customer.Id == customerId && customer.OrganizationId == organizationId));

    public Task<IReadOnlyList<Customer>> GetByOrganizationIdAsync(string organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Customer>>(Customers.Where(customer => customer.OrganizationId == organizationId).ToList());

    public Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var index = Customers.FindIndex(existing => existing.Id == customer.Id && existing.OrganizationId == customer.OrganizationId);
        if (index >= 0)
        {
            Customers[index] = customer;
        }

        return Task.FromResult(customer);
    }
}
