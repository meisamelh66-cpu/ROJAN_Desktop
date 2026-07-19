using Rojan.Desktop.Application.Customers;
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
            "Prefers evening appointments.");
        var repository = new StubCustomerRepository([domainCustomer]);
        var sut = new CustomerQueryService(repository);

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
        var sut = new CustomerQueryService(repository);

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
            domainStatus, "$0", DateTimeOffset.UnixEpoch, string.Empty);
        var repository = new StubCustomerRepository([domainCustomer]);
        var sut = new CustomerQueryService(repository);

        var result = await sut.GetCustomersAsync();

        Assert.Equal(expectedStatus, Assert.Single(result).Status);
    }

    private static IReadOnlyList<DomainCustomers.Customer> MakeSearchFixture() =>
    [
        new("customer-1", "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", string.Empty,
            DomainCustomers.CustomerStatus.Active, "$0", DateTimeOffset.UnixEpoch, string.Empty),
        new("customer-2", "Noah Bennett", string.Empty, "noah.bennett@example.com", string.Empty,
            DomainCustomers.CustomerStatus.Lead, "$0", DateTimeOffset.UnixEpoch, string.Empty),
        new("customer-3", "Sophia Reyes", "Reyes Beauty Studio", "sophia.reyes@example.com", string.Empty,
            DomainCustomers.CustomerStatus.Vip, "$0", DateTimeOffset.UnixEpoch, string.Empty),
    ];

    [Fact]
    public async Task SearchCustomersAsync_TextMatchesCompany_ReturnsOnlyThatCustomer()
    {
        var repository = new StubCustomerRepository(MakeSearchFixture());
        var sut = new CustomerQueryService(repository);

        var result = await sut.SearchCustomersAsync("reyes");

        Assert.Equal("customer-3", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchCustomersAsync_TextMatchesEmail_ReturnsOnlyThatCustomer()
    {
        var repository = new StubCustomerRepository(MakeSearchFixture());
        var sut = new CustomerQueryService(repository);

        var result = await sut.SearchCustomersAsync("noah.bennett");

        Assert.Equal("customer-2", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchCustomersAsync_EmptySearchText_ReturnsEveryCustomer()
    {
        var repository = new StubCustomerRepository(MakeSearchFixture());
        var sut = new CustomerQueryService(repository);

        var result = await sut.SearchCustomersAsync(string.Empty);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task SearchCustomersAsync_NoMatch_ReturnsEmptyList()
    {
        var repository = new StubCustomerRepository(MakeSearchFixture());
        var sut = new CustomerQueryService(repository);

        var result = await sut.SearchCustomersAsync("no-such-customer");

        Assert.Empty(result);
    }
}
