using Rojan.Desktop.Application.Inventory;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Tests.Organizations;

namespace Rojan.Desktop.Application.Tests.Inventory;

/// <summary>Exercises <see cref="InventoryCommandServicePermissionGate"/> - same "unauthorized never reaches the inner service" proof as <c>Customers.CustomerCommandServicePermissionGateTests</c>.</summary>
public sealed class InventoryCommandServicePermissionGateTests
{
    private static InventoryCommandServicePermissionGate CreateSut(WorkspaceRole role, StubInventoryRepository repository) =>
        new(
            new InventoryCommandService(repository, new StubEnterpriseContext { CurrentRole = role }),
            new PermissionGate(new PermissionEngine(), new StubEnterpriseContext { CurrentRole = role }));

    [Fact]
    public async Task CreateCategoryAsync_ReceptionRole_ThrowsAndNeverCreatesCategory()
    {
        var repository = new StubInventoryRepository();
        var sut = CreateSut(WorkspaceRole.Reception, repository);

        await Assert.ThrowsAsync<UnauthorizedOperationException>(() => sut.CreateCategoryAsync("Hair Care", "Shampoos"));

        Assert.Empty(repository.Categories);
    }

    [Fact]
    public async Task CreateCategoryAsync_InventoryRole_CreatesCategory()
    {
        var repository = new StubInventoryRepository();
        var sut = CreateSut(WorkspaceRole.Inventory, repository);

        var created = await sut.CreateCategoryAsync("Hair Care", "Shampoos");

        Assert.Equal("Hair Care", created.Name);
        Assert.Single(repository.Categories);
    }
}
