using Rojan.Server.Application.Customers;
using Rojan.Server.Application.Tests.Tenancy;
using Rojan.Server.Domain.Authentication;
using Rojan.Server.Domain.Customers;

namespace Rojan.Server.Application.Tests.Customers;

/// <summary>Exercises <see cref="CustomerService"/> - including the "Application: tenant isolation / cross-organization access denied" requirements this commit's own task list calls out explicitly.</summary>
public sealed class CustomerServiceTests
{
    private readonly FakeTenantContext _tenantContext = new();
    private readonly FakeCustomerRepository _customerRepository = new();
    private readonly FakeBranchRepository _branchRepository = new();

    private CustomerService CreateSut() => new(_tenantContext, _customerRepository, _branchRepository);

    private static CreateCustomerRequest NewCreateRequest(string? branchId = null) =>
        new("Noah Bennett", "555-0100", "noah@rojan.example", branchId);

    [Fact]
    public async Task CreateCustomerAsync_ValidRequest_CreatesCustomerScopedToCurrentOrganization()
    {
        _tenantContext.OrganizationId = "org-1";
        var sut = CreateSut();

        var result = await sut.CreateCustomerAsync(NewCreateRequest());

        var stored = Assert.Single(_customerRepository.Customers);
        Assert.Equal("org-1", stored.OrganizationId);
        Assert.Equal("Noah Bennett", stored.Name);
        Assert.Equal("555-0100", stored.Phone);
        Assert.Equal("noah@rojan.example", stored.Email);
        Assert.Equal(CustomerStatus.Active, stored.Status);
        Assert.Equal(stored.Id, result.Id);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task CreateCustomerAsync_WithBranchBelongingToOwnOrganization_Succeeds()
    {
        _tenantContext.OrganizationId = "org-1";
        _branchRepository.Branches.Add(new Branch("branch-1", "org-1", "Downtown", BranchStatus.Active, DateTimeOffset.UtcNow));
        var sut = CreateSut();

        var result = await sut.CreateCustomerAsync(NewCreateRequest("branch-1"));

        Assert.Equal("branch-1", result.BranchId);
    }

    [Fact]
    public async Task CreateCustomerAsync_WithBranchBelongingToDifferentOrganization_ThrowsInvalidCustomerBranchException()
    {
        _tenantContext.OrganizationId = "org-1";
        _branchRepository.Branches.Add(new Branch("branch-1", "org-2", "Someone Else's Branch", BranchStatus.Active, DateTimeOffset.UtcNow));
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidCustomerBranchException>(() => sut.CreateCustomerAsync(NewCreateRequest("branch-1")));

        Assert.Empty(_customerRepository.Customers);
    }

    [Fact]
    public async Task CreateCustomerAsync_WithNonexistentBranch_ThrowsInvalidCustomerBranchException()
    {
        _tenantContext.OrganizationId = "org-1";
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidCustomerBranchException>(() => sut.CreateCustomerAsync(NewCreateRequest("missing-branch")));
    }

    [Fact]
    public async Task GetCustomerAsync_CustomerBelongsToCurrentOrganization_ReturnsDto()
    {
        _tenantContext.OrganizationId = "org-1";
        var now = DateTimeOffset.UtcNow;
        _customerRepository.Customers.Add(new Customer("customer-1", "org-1", null, "Noah Bennett", "555-0100", null, CustomerStatus.Active, now, now));
        var sut = CreateSut();

        var result = await sut.GetCustomerAsync("customer-1");

        Assert.Equal("customer-1", result.Id);
        Assert.Equal("Noah Bennett", result.Name);
    }

    [Fact]
    public async Task GetCustomerAsync_CustomerBelongsToDifferentOrganization_ThrowsCustomerNotFoundException()
    {
        // The core tenant-isolation guarantee: Tenant A must never be able to read Tenant B's customer.
        _tenantContext.OrganizationId = "org-1";
        var now = DateTimeOffset.UtcNow;
        _customerRepository.Customers.Add(new Customer("customer-1", "org-2", null, "Someone Else's Customer", "555-0200", null, CustomerStatus.Active, now, now));
        var sut = CreateSut();

        await Assert.ThrowsAsync<CustomerNotFoundException>(() => sut.GetCustomerAsync("customer-1"));
    }

    [Fact]
    public async Task GetCustomerAsync_CustomerDoesNotExistAtAll_ThrowsCustomerNotFoundException()
    {
        _tenantContext.OrganizationId = "org-1";
        var sut = CreateSut();

        await Assert.ThrowsAsync<CustomerNotFoundException>(() => sut.GetCustomerAsync("missing-customer"));
    }

    [Fact]
    public async Task GetCustomersAsync_ReturnsOnlyCustomersForTheCurrentOrganization()
    {
        _tenantContext.OrganizationId = "org-1";
        var now = DateTimeOffset.UtcNow;
        _customerRepository.Customers.Add(new Customer("customer-1", "org-1", null, "Noah Bennett", "555-0100", null, CustomerStatus.Active, now, now));
        _customerRepository.Customers.Add(new Customer("customer-2", "org-1", null, "Ava Chen", "555-0101", null, CustomerStatus.Active, now, now));
        _customerRepository.Customers.Add(new Customer("customer-3", "org-2", null, "A Different Tenant's Customer", "555-0300", null, CustomerStatus.Active, now, now));
        var sut = CreateSut();

        var results = await sut.GetCustomersAsync();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, customer => customer.Id == "customer-1");
        Assert.Contains(results, customer => customer.Id == "customer-2");
        Assert.DoesNotContain(results, customer => customer.Id == "customer-3");
    }

    private static UpdateCustomerRequest NewUpdateRequest(string status = "Active", string? branchId = null) =>
        new("Noah Bennett-Chen", "555-0199", "noah.updated@rojan.example", branchId, status);

    [Fact]
    public async Task UpdateCustomerAsync_ValidRequest_UpdatesFieldsAndPreservesOrganizationId()
    {
        _tenantContext.OrganizationId = "org-1";
        var now = DateTimeOffset.UtcNow;
        _customerRepository.Customers.Add(new Customer("customer-1", "org-1", null, "Noah Bennett", "555-0100", null, CustomerStatus.Active, now, now));
        var sut = CreateSut();

        var result = await sut.UpdateCustomerAsync("customer-1", NewUpdateRequest());

        Assert.Equal("Noah Bennett-Chen", result.Name);
        Assert.Equal("555-0199", result.Phone);
        Assert.Equal("noah.updated@rojan.example", result.Email);
        Assert.Equal("org-1", Assert.Single(_customerRepository.Customers).OrganizationId);
    }

    [Fact]
    public async Task UpdateCustomerAsync_CustomerBelongsToDifferentOrganization_ThrowsCustomerNotFoundExceptionAndChangesNothing()
    {
        // Cross-organization access denied - Tenant A must not be able to modify Tenant B's customer,
        // and must get the same "not found" response as if it never existed.
        _tenantContext.OrganizationId = "org-1";
        var now = DateTimeOffset.UtcNow;
        var foreignCustomer = new Customer("customer-1", "org-2", null, "Someone Else's Customer", "555-0200", null, CustomerStatus.Active, now, now);
        _customerRepository.Customers.Add(foreignCustomer);
        var sut = CreateSut();

        await Assert.ThrowsAsync<CustomerNotFoundException>(() => sut.UpdateCustomerAsync("customer-1", NewUpdateRequest()));

        Assert.Equal(foreignCustomer, Assert.Single(_customerRepository.Customers));
    }

    [Fact]
    public async Task UpdateCustomerAsync_UnrecognizedStatus_ThrowsInvalidCustomerStatusException()
    {
        _tenantContext.OrganizationId = "org-1";
        var now = DateTimeOffset.UtcNow;
        _customerRepository.Customers.Add(new Customer("customer-1", "org-1", null, "Noah Bennett", "555-0100", null, CustomerStatus.Active, now, now));
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidCustomerStatusException>(() => sut.UpdateCustomerAsync("customer-1", NewUpdateRequest(status: "NotARealStatus")));
    }

    [Fact]
    public async Task UpdateCustomerAsync_SameStatus_DoesNotThrowAndUpdatesOtherFields()
    {
        _tenantContext.OrganizationId = "org-1";
        var now = DateTimeOffset.UtcNow;
        _customerRepository.Customers.Add(new Customer("customer-1", "org-1", null, "Noah Bennett", "555-0100", null, CustomerStatus.Active, now, now));
        var sut = CreateSut();

        var result = await sut.UpdateCustomerAsync("customer-1", NewUpdateRequest(status: "Active"));

        Assert.Equal("Noah Bennett-Chen", result.Name);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task UpdateCustomerAsync_WithBranchBelongingToDifferentOrganization_ThrowsInvalidCustomerBranchException()
    {
        _tenantContext.OrganizationId = "org-1";
        var now = DateTimeOffset.UtcNow;
        _customerRepository.Customers.Add(new Customer("customer-1", "org-1", null, "Noah Bennett", "555-0100", null, CustomerStatus.Active, now, now));
        _branchRepository.Branches.Add(new Branch("branch-1", "org-2", "Someone Else's Branch", BranchStatus.Active, DateTimeOffset.UtcNow));
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidCustomerBranchException>(() => sut.UpdateCustomerAsync("customer-1", NewUpdateRequest(branchId: "branch-1")));
    }
}
