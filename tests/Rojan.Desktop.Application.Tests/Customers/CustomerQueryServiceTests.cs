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
    [InlineData(DomainCustomers.CustomerStatus.Active, CustomerStatus.Active)]
    [InlineData(DomainCustomers.CustomerStatus.Inactive, CustomerStatus.Inactive)]
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
}
