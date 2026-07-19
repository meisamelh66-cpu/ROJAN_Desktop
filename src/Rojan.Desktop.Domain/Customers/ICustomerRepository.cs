namespace Rojan.Desktop.Domain.Customers;

/// <summary>
/// Repository abstraction for customer data. Domain defines the contract;
/// Infrastructure provides the concrete implementation (a fake/in-memory
/// one for now - Phase 09 explicitly has no backend integration yet, same
/// as the Dashboard vertical slice in Phase 06B).
/// </summary>
public interface ICustomerRepository
{
    public Task<IReadOnlyList<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default);

    public Task<Customer?> GetCustomerByIdAsync(string customerId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<CustomerNote>> GetNotesAsync(string customerId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<CustomerTag>> GetTagsAsync(string customerId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<CustomerActivity>> GetActivityAsync(string customerId, CancellationToken cancellationToken = default);

    public Task<Customer> CreateCustomerAsync(Customer customer, CancellationToken cancellationToken = default);

    public Task<Customer> UpdateCustomerAsync(Customer customer, CancellationToken cancellationToken = default);

    public Task<CustomerNote> AddNoteAsync(CustomerNote note, CancellationToken cancellationToken = default);

    public Task<CustomerTag> AddTagAsync(CustomerTag tag, CancellationToken cancellationToken = default);

    public Task RemoveTagAsync(string customerId, string tagId, CancellationToken cancellationToken = default);

    public Task<CustomerActivity> AddActivityAsync(CustomerActivity activity, CancellationToken cancellationToken = default);
}
