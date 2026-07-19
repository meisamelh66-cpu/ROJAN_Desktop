using Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Application.Tests.Customers;

/// <summary>Configurable <see cref="ICustomerRepository"/> test double - hands back exactly what each test configures, no hidden behavior.</summary>
internal sealed class StubCustomerRepository : ICustomerRepository
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<Customer>>> _getCustomers;

    public StubCustomerRepository(Func<CancellationToken, Task<IReadOnlyList<Customer>>> getCustomers)
    {
        _getCustomers = getCustomers;
    }

    public StubCustomerRepository(IReadOnlyList<Customer> customers)
        : this(_ => Task.FromResult(customers))
    {
    }

    public Task<IReadOnlyList<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default) =>
        _getCustomers(cancellationToken);
}
