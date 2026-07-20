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
    public async Task CreateCustomerAsync_ValidRequest_LogsCreationActivity()
    {
        var repository = new StubCustomerRepository();
        var sut = new CustomerCommandService(repository, new StubEnterpriseContext());
        var request = new CreateCustomerRequest("Noah Bennett", string.Empty, "noah@example.com", "555-0100", string.Empty);

        var created = await sut.CreateCustomerAsync(request);

        var activity = Assert.Single(repository.Activities);
        Assert.Equal(created.Id, activity.CustomerId);
        Assert.Equal("Customer created", activity.Description);
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
    public async Task UpdateCustomerAsync_ValidRequest_LogsUpdateActivity()
    {
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var sut = new CustomerCommandService(repository, new StubEnterpriseContext());
        var request = new UpdateCustomerRequest("customer-1", "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", "+1 555 010 2231", CustomerStatus.Vip, "$9,000", "Upgraded");

        await sut.UpdateCustomerAsync(request);

        var activity = Assert.Single(repository.Activities);
        Assert.Equal("customer-1", activity.CustomerId);
        Assert.Equal("Customer profile updated", activity.Description);
    }

    [Fact]
    public async Task AddNoteAsync_ValidText_AddsNoteAndLogsActivity()
    {
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var sut = new CustomerCommandService(repository, new StubEnterpriseContext());

        var note = await sut.AddNoteAsync("customer-1", "Prefers evening appointments.");

        Assert.Equal("Prefers evening appointments.", note.Text);
        Assert.Single(repository.Notes);
        Assert.Contains(repository.Activities, activity => activity.Description == "Note added");
    }

    [Fact]
    public async Task AddTagAsync_ValidLabel_AddsTagAndLogsActivity()
    {
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var sut = new CustomerCommandService(repository, new StubEnterpriseContext());

        var tag = await sut.AddTagAsync("customer-1", "VIP");

        Assert.Equal("VIP", tag.Label);
        Assert.Single(repository.Tags);
        Assert.Contains(repository.Activities, activity => activity.Description == "Tag added: VIP");
    }

    [Fact]
    public async Task RemoveTagAsync_ExistingTag_RemovesTagAndLogsActivity()
    {
        var repository = new StubCustomerRepository([MakeCustomer()]);
        repository.Tags.Add(new DomainCustomers.CustomerTag("tag-1", "customer-1", "VIP", DateTimeOffset.UnixEpoch));
        var sut = new CustomerCommandService(repository, new StubEnterpriseContext());

        await sut.RemoveTagAsync("customer-1", "tag-1");

        Assert.Empty(repository.Tags);
        Assert.Contains(repository.Activities, activity => activity.Description == "Tag removed");
    }
}
