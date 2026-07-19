using Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Application.Tests.Customers;

/// <summary>
/// In-memory, mutable <see cref="ICustomerRepository"/> test double -
/// exposes its backing lists directly so tests can both seed state and
/// assert on what a command wrote (e.g. that a command also logged an
/// activity entry), without a mocking library.
/// </summary>
internal sealed class StubCustomerRepository : ICustomerRepository
{
    public List<Customer> Customers { get; } = [];

    public List<CustomerNote> Notes { get; } = [];

    public List<CustomerTag> Tags { get; } = [];

    public List<CustomerActivity> Activities { get; } = [];

    public StubCustomerRepository()
    {
    }

    public StubCustomerRepository(IReadOnlyList<Customer> customers)
    {
        Customers.AddRange(customers);
    }

    public Task<IReadOnlyList<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Customer>>(Customers.ToList());

    public Task<Customer?> GetCustomerByIdAsync(string customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Customers.FirstOrDefault(customer => customer.Id == customerId));

    public Task<IReadOnlyList<CustomerNote>> GetNotesAsync(string customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CustomerNote>>(Notes.Where(note => note.CustomerId == customerId).ToList());

    public Task<IReadOnlyList<CustomerTag>> GetTagsAsync(string customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CustomerTag>>(Tags.Where(tag => tag.CustomerId == customerId).ToList());

    public Task<IReadOnlyList<CustomerActivity>> GetActivityAsync(string customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CustomerActivity>>(Activities.Where(activity => activity.CustomerId == customerId).ToList());

    public Task<Customer> CreateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        Customers.Add(customer);
        return Task.FromResult(customer);
    }

    public Task<Customer> UpdateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var index = Customers.FindIndex(existing => existing.Id == customer.Id);
        if (index >= 0)
        {
            Customers[index] = customer;
        }

        return Task.FromResult(customer);
    }

    public Task<CustomerNote> AddNoteAsync(CustomerNote note, CancellationToken cancellationToken = default)
    {
        Notes.Add(note);
        return Task.FromResult(note);
    }

    public Task<CustomerTag> AddTagAsync(CustomerTag tag, CancellationToken cancellationToken = default)
    {
        Tags.Add(tag);
        return Task.FromResult(tag);
    }

    public Task RemoveTagAsync(string customerId, string tagId, CancellationToken cancellationToken = default)
    {
        Tags.RemoveAll(tag => tag.CustomerId == customerId && tag.Id == tagId);
        return Task.CompletedTask;
    }

    public Task<CustomerActivity> AddActivityAsync(CustomerActivity activity, CancellationToken cancellationToken = default)
    {
        Activities.Add(activity);
        return Task.FromResult(activity);
    }
}
