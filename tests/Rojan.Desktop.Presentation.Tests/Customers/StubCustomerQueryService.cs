using Rojan.Desktop.Application.Customers;

namespace Rojan.Desktop.Presentation.Tests.Customers;

/// <summary>
/// Configurable <see cref="ICustomerQueryService"/> test double. Each test
/// supplies exactly the completion behavior it needs: an already-completed
/// Task to observe a synchronous Loaded/Empty/Error transition, or a
/// pending Task backed by a TaskCompletionSource to observe the Loading
/// state before completing it - see CustomerPageViewModelTests for both.
/// <see cref="SearchCustomersAsync(CustomerSearchFilter, CancellationToken)"/>
/// defaults to filtering whatever GetCustomersAsync would return (matching
/// CustomerQueryService's own real behavior), so tests that don't care
/// about filtering don't need to configure it separately;
/// <see cref="SearchCalls"/> records every filter it was asked to search
/// with, in call order, so ViewModel tests can assert on the composed
/// <see cref="CustomerSearchFilter"/> without needing a real filtering
/// implementation here - same reasoning as Bookings.StubBookingQueryService.
/// </summary>
internal sealed class StubCustomerQueryService : ICustomerQueryService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<CustomerDto>>> _getCustomers;
    private readonly Func<string, CancellationToken, Task<IReadOnlyList<CustomerDto>>>? _searchCustomers;
    private readonly Func<CustomerSearchFilter, CancellationToken, Task<IReadOnlyList<CustomerDto>>> _searchCustomersByFilter;

    /// <summary>Every filter this stub was asked to search with, in call order.</summary>
    public List<CustomerSearchFilter> SearchCalls { get; } = [];

    public StubCustomerQueryService(
        Func<CancellationToken, Task<IReadOnlyList<CustomerDto>>> getCustomers,
        Func<string, CancellationToken, Task<IReadOnlyList<CustomerDto>>>? searchCustomers = null,
        Func<CustomerSearchFilter, CancellationToken, Task<IReadOnlyList<CustomerDto>>>? searchCustomersByFilter = null)
    {
        _getCustomers = getCustomers;
        _searchCustomers = searchCustomers;

        // Defaults to ignoring the filter and returning the same fixed list
        // GetCustomersAsync would - every existing test that only supplies
        // getCustomers keeps working unchanged now that CustomerPageViewModel
        // calls SearchCustomersAsync(CustomerSearchFilter) instead of
        // GetCustomersAsync for every load (Sprint 4 Commit 2).
        _searchCustomersByFilter = searchCustomersByFilter ?? ((_, cancellationToken) => _getCustomers(cancellationToken));
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

    public Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(CustomerSearchFilter filter, CancellationToken cancellationToken = default)
    {
        SearchCalls.Add(filter);
        return _searchCustomersByFilter(filter, cancellationToken);
    }
}
