using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Application.Tests.Organizations;
using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Application.Tests.Customers;

public sealed class CustomerQueryServiceTests
{
    [Fact]
    public async Task GetCustomersAsync_RepositoryReturnsCustomers_MapsEveryFieldToDto()
    {
        var lastContacted = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
        var domainCustomer = new DomainCustomers.Customer(
            "customer-1",
            "Amelia Hart",
            "Hart & Co. Salon",
            "amelia.hart@example.com",
            "+1 (555) 010-2231",
            DomainCustomers.CustomerStatus.Active,
            "$4,820",
            lastContacted,
            "Prefers evening appointments.",
            "org-1",
            "branch-1");
        var repository = new StubCustomerRepository([domainCustomer]);
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext());

        var result = await sut.GetCustomersAsync();

        var dto = Assert.Single(result);
        Assert.Equal(domainCustomer.Id, dto.Id);
        Assert.Equal(domainCustomer.FullName, dto.FullName);
        Assert.Equal(domainCustomer.Company, dto.Company);
        Assert.Equal(domainCustomer.Email, dto.Email);
        Assert.Equal(domainCustomer.Phone, dto.Phone);
        Assert.Equal(CustomerStatus.Active, dto.Status);
        Assert.Equal(domainCustomer.LifetimeValue, dto.LifetimeValue);
        Assert.Equal(domainCustomer.LastContactedAt, dto.LastContactedAt);
        Assert.Equal(domainCustomer.Notes, dto.Notes);
    }

    [Fact]
    public async Task GetCustomersAsync_RepositoryReturnsEmptyList_ReturnsEmptyList()
    {
        var repository = new StubCustomerRepository([]);
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext());

        var result = await sut.GetCustomersAsync();

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(DomainCustomers.CustomerStatus.Lead, CustomerStatus.Lead)]
    [InlineData(DomainCustomers.CustomerStatus.Prospect, CustomerStatus.Prospect)]
    [InlineData(DomainCustomers.CustomerStatus.Active, CustomerStatus.Active)]
    [InlineData(DomainCustomers.CustomerStatus.Vip, CustomerStatus.Vip)]
    [InlineData(DomainCustomers.CustomerStatus.Inactive, CustomerStatus.Inactive)]
    [InlineData(DomainCustomers.CustomerStatus.Churned, CustomerStatus.Churned)]
    public async Task GetCustomersAsync_EachDomainStatus_MapsToMatchingApplicationStatus(
        DomainCustomers.CustomerStatus domainStatus, CustomerStatus expectedStatus)
    {
        var domainCustomer = new DomainCustomers.Customer(
            "customer-1", "Test Customer", string.Empty, "test@example.com", string.Empty,
            domainStatus, "$0", DateTimeOffset.UnixEpoch, string.Empty, "org-1", "branch-1");
        var repository = new StubCustomerRepository([domainCustomer]);
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext());

        var result = await sut.GetCustomersAsync();

        Assert.Equal(expectedStatus, Assert.Single(result).Status);
    }

    private static IReadOnlyList<DomainCustomers.Customer> MakeSearchFixture() =>
    [
        new("customer-1", "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", string.Empty,
            DomainCustomers.CustomerStatus.Active, "$0", DateTimeOffset.UnixEpoch, string.Empty, "org-1", "branch-1"),
        new("customer-2", "Noah Bennett", string.Empty, "noah.bennett@example.com", string.Empty,
            DomainCustomers.CustomerStatus.Lead, "$0", DateTimeOffset.UnixEpoch, string.Empty, "org-1", "branch-1"),
        new("customer-3", "Sophia Reyes", "Reyes Beauty Studio", "sophia.reyes@example.com", string.Empty,
            DomainCustomers.CustomerStatus.Vip, "$0", DateTimeOffset.UnixEpoch, string.Empty, "org-1", "branch-1"),
    ];

    [Fact]
    public async Task SearchCustomersAsync_TextMatchesCompany_ReturnsOnlyThatCustomer()
    {
        var repository = new StubCustomerRepository(MakeSearchFixture());
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext());

        var result = await sut.SearchCustomersAsync("reyes");

        Assert.Equal("customer-3", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchCustomersAsync_TextMatchesEmail_ReturnsOnlyThatCustomer()
    {
        var repository = new StubCustomerRepository(MakeSearchFixture());
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext());

        var result = await sut.SearchCustomersAsync("noah.bennett");

        Assert.Equal("customer-2", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchCustomersAsync_EmptySearchText_ReturnsEveryCustomer()
    {
        var repository = new StubCustomerRepository(MakeSearchFixture());
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext());

        var result = await sut.SearchCustomersAsync(string.Empty);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task SearchCustomersAsync_NoMatch_ReturnsEmptyList()
    {
        var repository = new StubCustomerRepository(MakeSearchFixture());
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext());

        var result = await sut.SearchCustomersAsync("no-such-customer");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCustomersAsync_CustomerInDifferentOrganization_IsExcluded()
    {
        var domainCustomer = new DomainCustomers.Customer(
            "customer-99", "Other Org Customer", string.Empty, "other@example.com", string.Empty,
            DomainCustomers.CustomerStatus.Active, "$0", DateTimeOffset.UnixEpoch, string.Empty, "org-2", "branch-3");
        var repository = new StubCustomerRepository([domainCustomer]);
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext { CurrentOrganizationId = "org-1", CurrentBranchId = "branch-1" });

        var result = await sut.GetCustomersAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCustomersAsync_CustomerInDifferentBranchSameOrganization_IsExcluded()
    {
        var domainCustomer = new DomainCustomers.Customer(
            "customer-98", "Other Branch Customer", string.Empty, "other@example.com", string.Empty,
            DomainCustomers.CustomerStatus.Active, "$0", DateTimeOffset.UnixEpoch, string.Empty, "org-1", "branch-2");
        var repository = new StubCustomerRepository([domainCustomer]);
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext { CurrentOrganizationId = "org-1", CurrentBranchId = "branch-1" });

        var result = await sut.GetCustomersAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCustomersAsync_NoBranchSelected_ReturnsEveryBranchWithinTheOrganization()
    {
        var branch1Customer = new DomainCustomers.Customer(
            "customer-a", "Branch 1 Customer", string.Empty, "a@example.com", string.Empty,
            DomainCustomers.CustomerStatus.Active, "$0", DateTimeOffset.UnixEpoch, string.Empty, "org-1", "branch-1");
        var branch2Customer = new DomainCustomers.Customer(
            "customer-b", "Branch 2 Customer", string.Empty, "b@example.com", string.Empty,
            DomainCustomers.CustomerStatus.Active, "$0", DateTimeOffset.UnixEpoch, string.Empty, "org-1", "branch-2");
        var repository = new StubCustomerRepository([branch1Customer, branch2Customer]);
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext { CurrentOrganizationId = "org-1", CurrentBranchId = null });

        var result = await sut.GetCustomersAsync();

        Assert.Equal(2, result.Count);
    }

    // Sprint 4 Commit 2: customer search and filters.

    [Fact]
    public async Task SearchCustomersAsync_EmptyFilter_ReturnsEveryCustomerSameAsGetCustomersAsync()
    {
        var repository = new StubCustomerRepository(MakeSearchFixture());
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext());

        var filtered = await sut.SearchCustomersAsync(CustomerSearchFilter.Empty);
        var unfiltered = await sut.GetCustomersAsync();

        Assert.Equal(unfiltered.Select(c => c.Id), filtered.Select(c => c.Id));
    }

    [Fact]
    public async Task SearchCustomersAsync_FilterSearchText_ReturnsMatchingCustomersOnly()
    {
        var repository = new StubCustomerRepository(MakeSearchFixture());
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext());

        var result = await sut.SearchCustomersAsync(new CustomerSearchFilter(SearchText: "reyes"));

        Assert.Equal("customer-3", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchCustomersAsync_FilterStatus_ReturnsOnlyCustomersWithThatStatus()
    {
        var repository = new StubCustomerRepository(MakeSearchFixture());
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext());

        var result = await sut.SearchCustomersAsync(new CustomerSearchFilter(Status: CustomerStatus.Vip));

        Assert.Equal("customer-3", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchCustomersAsync_FilterCompany_ReturnsOnlyCustomersAtThatCompany()
    {
        var repository = new StubCustomerRepository(MakeSearchFixture());
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext());

        var result = await sut.SearchCustomersAsync(new CustomerSearchFilter(Company: "Hart"));

        Assert.Equal("customer-1", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchCustomersAsync_FilterTag_ReturnsOnlyCustomersWithThatTag()
    {
        var repository = new StubCustomerRepository(MakeSearchFixture());
        repository.Tags.Add(new DomainCustomers.CustomerTag("tag-1", "customer-2", "VIP", DateTimeOffset.UnixEpoch));
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext());

        var result = await sut.SearchCustomersAsync(new CustomerSearchFilter(Tag: "vip"));

        Assert.Equal("customer-2", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchCustomersAsync_CombinedFilters_AreAndedTogether()
    {
        var repository = new StubCustomerRepository(MakeSearchFixture());
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext());

        // customer-1 is Active with company "Hart & Co. Salon" - Active + a different company must match nobody.
        var result = await sut.SearchCustomersAsync(new CustomerSearchFilter(Status: CustomerStatus.Active, Company: "Reyes"));

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCustomersAsync_CombinedFilters_MatchWhenEveryCriterionAgrees()
    {
        var repository = new StubCustomerRepository(MakeSearchFixture());
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext());

        var result = await sut.SearchCustomersAsync(new CustomerSearchFilter(Status: CustomerStatus.Active, Company: "Hart & Co. Salon"));

        Assert.Equal("customer-1", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchCustomersAsync_FilterMatchesNoCustomer_ReturnsEmptyList()
    {
        var repository = new StubCustomerRepository(MakeSearchFixture());
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext());

        var result = await sut.SearchCustomersAsync(new CustomerSearchFilter(SearchText: "no-such-customer"));

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCustomersAsync_CustomerInDifferentOrganization_IsExcluded()
    {
        var domainCustomer = new DomainCustomers.Customer(
            "customer-99", "Other Org Customer", string.Empty, "other@example.com", string.Empty,
            DomainCustomers.CustomerStatus.Active, "$0", DateTimeOffset.UnixEpoch, string.Empty, "org-2", "branch-3");
        var repository = new StubCustomerRepository([domainCustomer]);
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext { CurrentOrganizationId = "org-1", CurrentBranchId = "branch-1" });

        var result = await sut.SearchCustomersAsync(CustomerSearchFilter.Empty);

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchCustomersAsync_CustomerInDifferentBranchSameOrganization_IsExcluded()
    {
        var domainCustomer = new DomainCustomers.Customer(
            "customer-98", "Other Branch Customer", string.Empty, "other@example.com", string.Empty,
            DomainCustomers.CustomerStatus.Active, "$0", DateTimeOffset.UnixEpoch, string.Empty, "org-1", "branch-2");
        var repository = new StubCustomerRepository([domainCustomer]);
        var sut = new CustomerQueryService(repository, new StubEnterpriseContext { CurrentOrganizationId = "org-1", CurrentBranchId = "branch-1" });

        var result = await sut.SearchCustomersAsync(CustomerSearchFilter.Empty);

        Assert.Empty(result);
    }
}
