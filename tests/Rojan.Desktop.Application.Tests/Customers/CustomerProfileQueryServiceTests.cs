using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Application.Tests.Organizations;
using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Application.Tests.Customers;

public sealed class CustomerProfileQueryServiceTests
{
    private static DomainCustomers.Customer MakeCustomer(string id = "customer-1") =>
        new(id, "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", "+1 555 010 2231",
            DomainCustomers.CustomerStatus.Vip, "$4,820", new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero), "Notes", "org-1", "branch-1");

    [Fact]
    public async Task GetProfileAsync_CustomerExists_ReturnsCustomerNotesTagsAndActivity()
    {
        var repository = new StubCustomerRepository([MakeCustomer()]);
        repository.Notes.Add(new DomainCustomers.CustomerNote("note-1", "customer-1", "Allergic to certain dyes.", DateTimeOffset.UnixEpoch));
        repository.Tags.Add(new DomainCustomers.CustomerTag("tag-1", "customer-1", "VIP", DateTimeOffset.UnixEpoch));
        repository.Activities.Add(new DomainCustomers.CustomerActivity("activity-1", "customer-1", "Customer created", DateTimeOffset.UnixEpoch));
        var sut = new CustomerProfileQueryService(repository, new StubEnterpriseContext());

        var profile = await sut.GetProfileAsync("customer-1");

        Assert.Equal("customer-1", profile.Customer.Id);
        Assert.Equal("Allergic to certain dyes.", Assert.Single(profile.Notes).Text);
        Assert.Equal("VIP", Assert.Single(profile.Tags).Label);
        Assert.Equal("Customer created", Assert.Single(profile.Activity).Description);
    }

    [Fact]
    public async Task GetProfileAsync_CustomerHasNotesAndTags_StatisticsReflectTheirCounts()
    {
        var repository = new StubCustomerRepository([MakeCustomer()]);
        repository.Notes.Add(new DomainCustomers.CustomerNote("note-1", "customer-1", "Note one", DateTimeOffset.UnixEpoch));
        repository.Notes.Add(new DomainCustomers.CustomerNote("note-2", "customer-1", "Note two", DateTimeOffset.UnixEpoch));
        repository.Tags.Add(new DomainCustomers.CustomerTag("tag-1", "customer-1", "VIP", DateTimeOffset.UnixEpoch));
        var sut = new CustomerProfileQueryService(repository, new StubEnterpriseContext());

        var profile = await sut.GetProfileAsync("customer-1");

        Assert.Contains(profile.Statistics, stat => stat.Label == "Notes" && stat.Value == "2");
        Assert.Contains(profile.Statistics, stat => stat.Label == "Tags" && stat.Value == "1");
        Assert.Contains(profile.Statistics, stat => stat.Label == "Lifetime Value" && stat.Value == "$4,820");
        Assert.Contains(profile.Statistics, stat => stat.Label == "Status" && stat.Value == "Vip");
    }

    [Fact]
    public async Task GetProfileAsync_CustomerDoesNotExist_ThrowsInvalidOperationException()
    {
        var repository = new StubCustomerRepository([]);
        var sut = new CustomerProfileQueryService(repository, new StubEnterpriseContext());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetProfileAsync("missing-customer"));
    }

    [Fact]
    public async Task GetProfileAsync_CustomerBelongsToDifferentOrganization_ThrowsAsIfNotFound()
    {
        var repository = new StubCustomerRepository([MakeCustomer()]);
        var sut = new CustomerProfileQueryService(repository, new StubEnterpriseContext { CurrentOrganizationId = "org-2", CurrentBranchId = "branch-3" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetProfileAsync("customer-1"));
    }
}
