using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Tests.Organizations;

namespace Rojan.Desktop.Application.Tests.Customers;

/// <summary>Exercises <see cref="CustomerCommandServicePermissionGate"/> - proves an unauthorized role's call never reaches the wrapped service, and an authorized role's call passes through unchanged.</summary>
public sealed class CustomerCommandServicePermissionGateTests
{
    private static CustomerCommandServicePermissionGate CreateSut(WorkspaceRole role, StubCustomerRepository repository) =>
        new(
            new CustomerCommandService(repository, new StubEnterpriseContext { CurrentRole = role }),
            new PermissionGate(new PermissionEngine(), new StubEnterpriseContext { CurrentRole = role }));

    [Fact]
    public async Task CreateCustomerAsync_RoleWithoutCustomerEdit_ThrowsAndNeverCreatesCustomer()
    {
        var repository = new StubCustomerRepository();
        var sut = CreateSut(WorkspaceRole.Inventory, repository);
        var request = new CreateCustomerRequest("Noah Bennett", string.Empty, "noah@example.com", "555-0100", string.Empty);

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateCustomerAsync(request));

        Assert.Empty(repository.Customers);
    }

    [Fact]
    public async Task CreateCustomerAsync_RoleWithCustomerEdit_CreatesCustomer()
    {
        var repository = new StubCustomerRepository();
        var sut = CreateSut(WorkspaceRole.Reception, repository);
        var request = new CreateCustomerRequest("Noah Bennett", string.Empty, "noah@example.com", "555-0100", string.Empty);

        var created = await sut.CreateCustomerAsync(request);

        Assert.Equal("Noah Bennett", created.FullName);
        Assert.Single(repository.Customers);
    }
}
