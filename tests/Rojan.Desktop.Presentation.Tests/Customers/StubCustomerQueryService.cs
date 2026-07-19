using Rojan.Desktop.Application.Customers;

namespace Rojan.Desktop.Presentation.Tests.Customers;

/// <summary>
/// Configurable <see cref="ICustomerQueryService"/> test double. Each test
/// supplies exactly the completion behavior it needs: an already-completed
/// Task to observe a synchronous Loaded/Empty/Error transition, or a
/// pending Task backed by a TaskCompletionSource to observe the Loading
/// state before completing it - see CustomerPageViewModelTests for both.
/// </summary>
internal sealed class StubCustomerQueryService : ICustomerQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<CustomerDto>>> _getCustomers;

    public StubCustomerQueryService(Func<CancellationToken, Task<IReadOnlyList<CustomerDto>>> getCustomers)
    {
        _getCustomers = getCustomers;
    }

    public Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default) =>
        _getCustomers(cancellationToken);
}
