using Rojan.Desktop.Application.Customers;

namespace Rojan.Desktop.Application.Tests.BookingWorkflow;

/// <summary>Minimal <see cref="ICustomerQueryService"/> test double - only <see cref="GetCustomersAsync"/> is exercised by <see cref="BookingWorkflowServiceTests"/>, so <see cref="SearchCustomersAsync"/> just delegates to it.</summary>
internal sealed class StubCustomerQueryService : ICustomerQueryService
{
    private readonly IReadOnlyList<CustomerDto> _customers;

    public StubCustomerQueryService(IReadOnlyList<CustomerDto> customers)
    {
        _customers = customers;
    }

    public Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_customers);

    public Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(string searchText, CancellationToken cancellationToken = default) =>
        Task.FromResult(_customers);
}
