using Rojan.Desktop.Application.Customers;

namespace Rojan.Desktop.Presentation.Tests.Customers;

/// <summary>
/// Configurable <see cref="ICustomerQueryService"/> test double. Each test
/// supplies exactly the completion behavior it needs: an already-completed
/// Task to observe a synchronous Loaded/Empty/Error transition, or a
/// pending Task backed by a TaskCompletionSource to observe the Loading
/// state before completing it - see CustomerPageViewModelTests for both.
/// Search defaults to filtering whatever GetCustomersAsync would return
/// (matching CustomerQueryService's own real behavior), so tests that
/// don't care about search don't need to configure it separately.
/// </summary>
internal sealed class StubCustomerQueryService : ICustomerQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<CustomerDto>>> _getCustomers;
    private readonly Func<string, CancellationToken, Task<IReadOnlyList<CustomerDto>>>? _searchCustomers;

    public StubCustomerQueryService(
        Func<CancellationToken, Task<IReadOnlyList<CustomerDto>>> getCustomers,
        Func<string, CancellationToken, Task<IReadOnlyList<CustomerDto>>>? searchCustomers = null)
    {
        _getCustomers = getCustomers;
        _searchCustomers = searchCustomers;
    }

    public Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default) =>
        _getCustomers(cancellationToken);

    public async Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(string searchText, CancellationToken cancellationToken = default)
    {
        if (_searchCustomers is not null)
        {
            return await _searchCustomers(searchText, cancellationToken).ConfigureAwait(true);
        }

        var customers = await _getCustomers(cancellationToken).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return customers;
        }

        return customers
            .Where(customer =>
                customer.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                customer.Company.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                customer.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
