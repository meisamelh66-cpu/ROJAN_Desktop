namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Write use cases for Customers - the first command-side (as opposed to
/// query-side) service in this codebase, per the Phase 08 Testing
/// Strategy's own note that no CQRS command pattern existed yet. Presentation
/// depends on this, never on Domain/Infrastructure directly, same rule as
/// every query service.
/// </summary>
public interface ICustomerCommandService
{
    public Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    public Task<CustomerDto> UpdateCustomerAsync(UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    public Task<CustomerNoteDto> AddNoteAsync(string customerId, string text, CancellationToken cancellationToken = default);

    public Task<CustomerTagDto> AddTagAsync(string customerId, string label, CancellationToken cancellationToken = default);

    public Task RemoveTagAsync(string customerId, string tagId, CancellationToken cancellationToken = default);
}
