using Rojan.Desktop.Domain.Customers;
using Rojan.Desktop.Infrastructure.Customers;

namespace Rojan.Desktop.Infrastructure.Tests.Customers;

/// <summary>
/// Smoke coverage for the read side (FakeCustomerRepository is a hardcoded
/// stand-in by design, see its own doc comment - no mapping/business logic
/// to exercise beyond "it returns the right data" and "it honors
/// cancellation") plus behavioral coverage for the write side Phase 10
/// added (create/update/note/tag/activity all mutate real in-memory state
/// now, unlike the read-only Phase 09 fake).
/// </summary>
public sealed class FakeCustomerRepositoryTests
{
    [Fact]
    public async Task GetCustomersAsync_ReturnsNonEmptyList()
    {
        var sut = new FakeCustomerRepository();

        var result = await sut.GetCustomersAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetCustomersAsync_CancellationAlreadyRequested_ThrowsTaskCanceledException()
    {
        var sut = new FakeCustomerRepository();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => sut.GetCustomersAsync(cts.Token));
    }

    [Fact]
    public async Task GetCustomerByIdAsync_KnownId_ReturnsMatchingCustomer()
    {
        var sut = new FakeCustomerRepository();

        var customer = await sut.GetCustomerByIdAsync("customer-1");

        Assert.NotNull(customer);
        Assert.Equal("customer-1", customer.Id);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_UnknownId_ReturnsNull()
    {
        var sut = new FakeCustomerRepository();

        var customer = await sut.GetCustomerByIdAsync("no-such-customer");

        Assert.Null(customer);
    }

    [Fact]
    public async Task GetNotesAsync_KnownCustomer_ReturnsOnlyThatCustomersNotes()
    {
        var sut = new FakeCustomerRepository();

        var notes = await sut.GetNotesAsync("customer-1");

        Assert.NotEmpty(notes);
        Assert.All(notes, note => Assert.Equal("customer-1", note.CustomerId));
    }

    [Fact]
    public async Task GetTagsAsync_KnownCustomer_ReturnsOnlyThatCustomersTags()
    {
        var sut = new FakeCustomerRepository();

        var tags = await sut.GetTagsAsync("customer-1");

        Assert.NotEmpty(tags);
        Assert.All(tags, tag => Assert.Equal("customer-1", tag.CustomerId));
    }

    [Fact]
    public async Task GetActivityAsync_KnownCustomer_ReturnsOnlyThatCustomersActivityNewestFirst()
    {
        var sut = new FakeCustomerRepository();

        var activity = await sut.GetActivityAsync("customer-1");

        Assert.NotEmpty(activity);
        Assert.All(activity, entry => Assert.Equal("customer-1", entry.CustomerId));
        Assert.Equal(activity.OrderByDescending(entry => entry.OccurredAt), activity);
    }

    [Fact]
    public async Task CreateCustomerAsync_NewCustomer_BecomesVisibleViaGetCustomersAsync()
    {
        var sut = new FakeCustomerRepository();
        var newCustomer = new Customer("customer-new", "Test Customer", string.Empty, "test@example.com", string.Empty,
            CustomerStatus.Lead, "$0", DateTimeOffset.UnixEpoch, string.Empty, "org-1", "branch-1");

        await sut.CreateCustomerAsync(newCustomer);
        var customers = await sut.GetCustomersAsync();

        Assert.Contains(customers, customer => customer.Id == "customer-new");
    }

    [Fact]
    public async Task UpdateCustomerAsync_ExistingCustomer_ReplacesStoredRecord()
    {
        var sut = new FakeCustomerRepository();
        var existing = await sut.GetCustomerByIdAsync("customer-1");
        var updated = existing! with { Status = CustomerStatus.Churned, LifetimeValue = "$0" };

        await sut.UpdateCustomerAsync(updated);
        var reloaded = await sut.GetCustomerByIdAsync("customer-1");

        Assert.Equal(CustomerStatus.Churned, reloaded!.Status);
        Assert.Equal("$0", reloaded.LifetimeValue);
    }

    [Fact]
    public async Task UpdateCustomerAsync_UnknownCustomer_ThrowsInvalidOperationException()
    {
        var sut = new FakeCustomerRepository();
        var unknown = new Customer("no-such-customer", "Ghost", string.Empty, string.Empty, string.Empty,
            CustomerStatus.Lead, "$0", DateTimeOffset.UnixEpoch, string.Empty, "org-1", "branch-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateCustomerAsync(unknown));
    }

    [Fact]
    public async Task AddNoteAsync_NewNote_BecomesVisibleViaGetNotesAsync()
    {
        var sut = new FakeCustomerRepository();
        var note = new CustomerNote("note-new", "customer-1", "New note text", DateTimeOffset.UnixEpoch);

        await sut.AddNoteAsync(note);
        var notes = await sut.GetNotesAsync("customer-1");

        Assert.Contains(notes, n => n.Id == "note-new");
    }

    [Fact]
    public async Task AddTagAsync_NewTag_BecomesVisibleViaGetTagsAsync()
    {
        var sut = new FakeCustomerRepository();
        var tag = new CustomerTag("tag-new", "customer-1", "Priority", DateTimeOffset.UnixEpoch);

        await sut.AddTagAsync(tag);
        var tags = await sut.GetTagsAsync("customer-1");

        Assert.Contains(tags, t => t.Id == "tag-new");
    }

    [Fact]
    public async Task RemoveTagAsync_ExistingTag_NoLongerReturnedByGetTagsAsync()
    {
        var sut = new FakeCustomerRepository();
        var tag = new CustomerTag("tag-new", "customer-1", "Priority", DateTimeOffset.UnixEpoch);
        await sut.AddTagAsync(tag);

        await sut.RemoveTagAsync("customer-1", "tag-new");
        var tags = await sut.GetTagsAsync("customer-1");

        Assert.DoesNotContain(tags, t => t.Id == "tag-new");
    }

    [Fact]
    public async Task AddActivityAsync_NewEntry_BecomesVisibleViaGetActivityAsync()
    {
        var sut = new FakeCustomerRepository();
        var activity = new CustomerActivity("activity-new", "customer-1", "Test event", DateTimeOffset.UnixEpoch);

        await sut.AddActivityAsync(activity);
        var entries = await sut.GetActivityAsync("customer-1");

        Assert.Contains(entries, e => e.Id == "activity-new");
    }
}
