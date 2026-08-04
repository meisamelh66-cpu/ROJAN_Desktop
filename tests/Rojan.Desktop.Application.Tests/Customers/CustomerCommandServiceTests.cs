using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Application.Tests.Organizations;
using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Application.Tests.Customers;

public sealed class CustomerCommandServiceTests
{
    private static DomainCustomers.Customer MakeCustomer(string id = "customer-1") =>
        new(id, "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", "+1 555 010 2231",
            DomainCustomers.CustomerStatus.Active, "$4,820", DateTimeOffset.UnixEpoch, "Notes", "org-1", "branch-1");

    [Fact]
    public async Task CreateCustomerAsync_ValidRequest_AddsCustomerAsLead()
    {
        var repository = new StubCustomerRepository();
        var sut = new CustomerCommandService(repository, new StubEnterpriseContext());
        var request = new CreateCustomerRequest("Noah Bennett", string.Empty, "noah@example.com", "555-0100", "First contact");

        var created = await sut.CreateCustomerAsync(request);

        Assert.Equal("Noah Bennett", created.FullName);
        Assert.Equal(CustomerStatus.Lead, created.Status);
        Assert.Single(repository.Customers);
    }

    [Fact]
    public async Task CreateCustomerAsync_ValidRequest_DoesNotLogActivity()
    {
        // Backend has no generic "customer created" activity - see CustomerCommandService's own doc comment.
        var repository = new StubCustomerRepository();
        var sut = new CustomerCommandService(repository, new StubEnterpriseContext());
        var request = new CreateCustomerRequest("Noah Bennett", string.Empty, "noah@example.com", "555-0100", string.Empty);

        await sut.CreateCustomerAsync(request);

        Assert.Empty(repository.Activities);
    }

    [Fact]
    public async Task UpdateCustomerAsync_ValidRequest_ReplacesCustomerFields()
    {
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var sut = new CustomerCommandService(repository, new StubEnterpriseContext());
        var request = new UpdateCustomerRequest("customer-1", "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", "+1 555 010 2231", CustomerStatus.Vip, "$9,000", "Upgraded");

        var updated = await sut.UpdateCustomerAsync(request);

        Assert.Equal(CustomerStatus.Vip, updated.Status);
        Assert.Equal("$9,000", updated.LifetimeValue);
        Assert.Equal(DomainCustomers.CustomerStatus.Vip, Assert.Single(repository.Customers).Status);
    }

    [Fact]
    public async Task UpdateCustomerAsync_ValidRequest_DoesNotLogActivity()
    {
        // Status change is logged by the backend's own UpdateCustomerUseCase - logging it again here would double it.
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var sut = new CustomerCommandService(repository, new StubEnterpriseContext());
        var request = new UpdateCustomerRequest("customer-1", "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", "+1 555 010 2231", CustomerStatus.Vip, "$9,000", "Upgraded");

        await sut.UpdateCustomerAsync(request);

        Assert.Empty(repository.Activities);
    }

    [Fact]
    public async Task AddNoteAsync_ValidText_AddsNoteWithoutLoggingActivity()
    {
        // A note already appears in the backend's merged timeline as its own entry - a separate "Note added" activity would be redundant.
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var sut = new CustomerCommandService(repository, new StubEnterpriseContext());

        var note = await sut.AddNoteAsync("customer-1", "Prefers evening appointments.");

        Assert.Equal("Prefers evening appointments.", note.Text);
        Assert.Single(repository.Notes);
        Assert.Empty(repository.Activities);
    }

    [Fact]
    public async Task AddTagAsync_ValidLabel_AddsTagWithoutLoggingActivity()
    {
        // Backend's AddCustomerTagUseCase logs TAG_ADDED itself - logging it again here would double it.
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var sut = new CustomerCommandService(repository, new StubEnterpriseContext());

        var tag = await sut.AddTagAsync("customer-1", "VIP");

        Assert.Equal("VIP", tag.Label);
        Assert.Single(repository.Tags);
        Assert.Empty(repository.Activities);
    }

    [Fact]
    public async Task RemoveTagAsync_ExistingTag_RemovesTagWithoutLoggingActivity()
    {
        // Backend's RemoveCustomerTagUseCase logs TAG_REMOVED itself - logging it again here would double it.
        var repository = new StubCustomerRepository([MakeCustomer()]);
        repository.Tags.Add(new DomainCustomers.CustomerTag("tag-1", "customer-1", "VIP", DateTimeOffset.UnixEpoch));
        var sut = new CustomerCommandService(repository, new StubEnterpriseContext());

        await sut.RemoveTagAsync("customer-1", "tag-1");

        Assert.Empty(repository.Tags);
        Assert.Empty(repository.Activities);
    }

    // Sprint 4 Commit 1: customer lifecycle rules.

    private static DomainCustomers.Customer MakeCustomerWithStatus(DomainCustomers.CustomerStatus status, string id = "customer-1") =>
        new(id, "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", "+1 555 010 2231",
            status, "$4,820", DateTimeOffset.UnixEpoch, "Notes", "org-1", "branch-1");

    [Fact]
    public async Task UpdateCustomerAsync_ValidStatusTransition_UpdatesStatus()
    {
        var repository = new StubCustomerRepository([MakeCustomerWithStatus(DomainCustomers.CustomerStatus.Lead)]);
        var sut = new CustomerCommandService(repository, new StubEnterpriseContext());
        var request = new UpdateCustomerRequest("customer-1", "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", "+1 555 010 2231", CustomerStatus.Prospect, "$0", "Notes");

        var updated = await sut.UpdateCustomerAsync(request);

        Assert.Equal(CustomerStatus.Prospect, updated.Status);
        Assert.Equal(DomainCustomers.CustomerStatus.Prospect, Assert.Single(repository.Customers).Status);
    }

    [Fact]
    public async Task UpdateCustomerAsync_InvalidStatusTransition_ThrowsInvalidOperationException()
    {
        // Lead -> Vip skips the relationship-building stages entirely - not a legal jump.
        var repository = new StubCustomerRepository([MakeCustomerWithStatus(DomainCustomers.CustomerStatus.Lead)]);
        var sut = new CustomerCommandService(repository, new StubEnterpriseContext());
        var request = new UpdateCustomerRequest("customer-1", "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", "+1 555 010 2231", CustomerStatus.Vip, "$0", "Notes");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateCustomerAsync(request));

        // "Failed update keeps original customer intact" - status must not have changed.
        Assert.Equal(DomainCustomers.CustomerStatus.Lead, Assert.Single(repository.Customers).Status);
    }

    [Fact]
    public async Task UpdateCustomerAsync_SameStatus_DoesNotThrowAndUpdatesOtherFields()
    {
        // The common case: editing name/company/email/phone/notes without touching status at all
        // must keep working exactly as before Sprint 4 Commit 1 - this is not a "transition".
        var repository = new StubCustomerRepository([MakeCustomerWithStatus(DomainCustomers.CustomerStatus.Active)]);
        var sut = new CustomerCommandService(repository, new StubEnterpriseContext());
        var request = new UpdateCustomerRequest("customer-1", "Amelia Hart-Bennett", "Hart & Co. Salon", "amelia.hart@example.com", "+1 555 010 2231", CustomerStatus.Active, "$4,820", "Updated notes");

        var updated = await sut.UpdateCustomerAsync(request);

        Assert.Equal("Amelia Hart-Bennett", updated.FullName);
        Assert.Equal("Updated notes", updated.Notes);
        Assert.Equal(CustomerStatus.Active, updated.Status);
    }

    [Theory]
    [InlineData(DomainCustomers.CustomerStatus.Lead, CustomerStatus.Inactive)]
    [InlineData(DomainCustomers.CustomerStatus.Churned, CustomerStatus.Active)]
    [InlineData(DomainCustomers.CustomerStatus.Vip, CustomerStatus.Lead)]
    public async Task UpdateCustomerAsync_VariousInvalidTransitions_ThrowInvalidOperationException(
        DomainCustomers.CustomerStatus currentStatus, CustomerStatus requestedStatus)
    {
        var repository = new StubCustomerRepository([MakeCustomerWithStatus(currentStatus)]);
        var sut = new CustomerCommandService(repository, new StubEnterpriseContext());
        var request = new UpdateCustomerRequest("customer-1", "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", "+1 555 010 2231", requestedStatus, "$0", "Notes");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UpdateCustomerAsync(request));
    }
}
