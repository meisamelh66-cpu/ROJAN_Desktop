using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Application.Tests.Organizations;
using Rojan.Desktop.Application.Tests.Security;
using Rojan.Desktop.Domain.Security;
using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Application.Tests.Customers;

/// <summary>
/// Exercises <see cref="CustomerCommandServiceSyncProducer"/> - proves a
/// successful create/update enqueues exactly one correctly-shaped
/// <see cref="PendingSyncOperation"/>, a failed command enqueues nothing,
/// and out-of-scope methods (notes/tags) never touch the queue.
/// </summary>
public sealed class CustomerCommandServiceSyncProducerTests
{
    private static DomainCustomers.Customer MakeCustomer(string id = "customer-1") =>
        new(id, "Noah Bennett", string.Empty, "noah@example.com", "555-0100",
            DomainCustomers.CustomerStatus.Lead, "0 تومان", DateTimeOffset.Now, string.Empty, "org-1", "branch-1");

    private static (CustomerCommandServiceSyncProducer Sut, StubCustomerRepository Repository, FakeSyncQueueService Queue) CreateSut(
        IReadOnlyList<DomainCustomers.Customer>? seed = null)
    {
        var repository = seed is null ? new StubCustomerRepository() : new StubCustomerRepository(seed);
        var inner = new CustomerCommandService(repository, new StubEnterpriseContext());
        var queue = new FakeSyncQueueService();
        return (new CustomerCommandServiceSyncProducer(inner, queue), repository, queue);
    }

    [Fact]
    public async Task CreateCustomerAsync_Succeeds_EnqueuesOneCreateSyncOperationWithCorrectEntityInformation()
    {
        var (sut, _, queue) = CreateSut();
        var request = new CreateCustomerRequest("Noah Bennett", string.Empty, "noah@example.com", "555-0100", string.Empty);

        var created = await sut.CreateCustomerAsync(request);

        var operation = Assert.Single(queue.Enqueued);
        Assert.Equal("Customer", operation.EntityType);
        Assert.Equal(created.Id, operation.EntityId);
        Assert.Equal("Create", operation.OperationType);
        Assert.Contains(created.Id, operation.Payload);
        Assert.True(operation.QueuedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task UpdateCustomerAsync_Succeeds_EnqueuesOneUpdateSyncOperation()
    {
        var (sut, _, queue) = CreateSut([MakeCustomer()]);
        var request = new UpdateCustomerRequest("customer-1", "Noah Bennett", "Acme", "noah@example.com", "555-0100",
            CustomerStatus.Lead, "0 تومان", string.Empty);

        var updated = await sut.UpdateCustomerAsync(request);

        var operation = Assert.Single(queue.Enqueued);
        Assert.Equal("Customer", operation.EntityType);
        Assert.Equal(updated.Id, operation.EntityId);
        Assert.Equal("Update", operation.OperationType);
    }

    [Fact]
    public async Task UpdateCustomerAsync_CustomerDoesNotExist_ThrowsAndEnqueuesNothing()
    {
        var (sut, _, queue) = CreateSut();
        var request = new UpdateCustomerRequest("missing-customer", "Noah Bennett", "Acme", "noah@example.com", "555-0100",
            CustomerStatus.Lead, "0 تومان", string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateCustomerAsync(request));

        Assert.Empty(queue.Enqueued);
    }

    [Fact]
    public async Task UpdateCustomerAsync_InvalidStatusTransition_ThrowsAndEnqueuesNothing()
    {
        var (sut, _, queue) = CreateSut([MakeCustomer()]);
        // Lead -> Vip is not a valid direct transition (see DomainCustomers.CustomerRules).
        var request = new UpdateCustomerRequest("customer-1", "Noah Bennett", string.Empty, "noah@example.com", "555-0100",
            CustomerStatus.Vip, "0 تومان", string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateCustomerAsync(request));

        Assert.Empty(queue.Enqueued);
    }

    [Fact]
    public async Task MultipleUpdates_EachSucceeds_EnqueuesOneOperationPerUpdateInOrder()
    {
        var (sut, _, queue) = CreateSut([MakeCustomer()]);
        var first = new UpdateCustomerRequest("customer-1", "Noah Bennett", string.Empty, "noah@example.com", "555-0100",
            CustomerStatus.Lead, "0 تومان", "First update");
        var second = new UpdateCustomerRequest("customer-1", "Noah Bennett", string.Empty, "noah@example.com", "555-0100",
            CustomerStatus.Lead, "0 تومان", "Second update");

        await sut.UpdateCustomerAsync(first);
        await sut.UpdateCustomerAsync(second);

        Assert.Equal(2, queue.Enqueued.Count);
        Assert.All(queue.Enqueued, operation => Assert.Equal("Update", operation.OperationType));
        Assert.NotEqual(queue.Enqueued[0].Id, queue.Enqueued[1].Id);
    }

    [Fact]
    public async Task AddNoteAsync_IsOutOfProducerScope_NeverEnqueuesASyncOperation()
    {
        var (sut, _, queue) = CreateSut([MakeCustomer()]);

        await sut.AddNoteAsync("customer-1", "Called to confirm appointment.");

        Assert.Empty(queue.Enqueued);
    }
}
