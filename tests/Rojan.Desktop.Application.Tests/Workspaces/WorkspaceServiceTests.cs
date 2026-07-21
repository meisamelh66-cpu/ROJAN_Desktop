using Rojan.Desktop.Application.Workspaces;

namespace Rojan.Desktop.Application.Tests.Workspaces;

/// <summary>Exercises <see cref="WorkspaceService"/>'s orchestration: first-run bootstrap, restore-active, CRUD, switch/recent tracking, save, and reset.</summary>
public sealed class WorkspaceServiceTests
{
    private static WorkspaceService CreateService(out FakeWorkspaceRepository repository)
    {
        repository = new FakeWorkspaceRepository();
        return new WorkspaceService(repository);
    }

    [Fact]
    public async Task EnsureInitializedAsync_NoExistingWorkspace_CreatesAndActivatesDefault()
    {
        var service = CreateService(out _);

        var workspace = await service.EnsureInitializedAsync("dashboard");

        Assert.Equal("dashboard", workspace.PrimaryModuleId);
        Assert.True(workspace.IsDefault);
        Assert.Null(workspace.SecondaryRoot);

        var active = await service.GetActiveWorkspaceAsync();
        Assert.Equal(workspace.Id, active.Id);
    }

    [Fact]
    public async Task EnsureInitializedAsync_CalledTwice_DoesNotCreateASecondDefault()
    {
        var service = CreateService(out _);
        await service.EnsureInitializedAsync("dashboard");

        await service.EnsureInitializedAsync("dashboard");

        var all = await service.GetWorkspacesAsync();
        Assert.Single(all);
    }

    [Fact]
    public async Task EnsureInitializedAsync_ActiveWorkspaceAlreadySet_ReturnsIt()
    {
        var service = CreateService(out _);
        var first = await service.EnsureInitializedAsync("dashboard");
        var created = await service.CreateWorkspaceAsync("Reception", "customers");
        await service.SwitchWorkspaceAsync(created.Id);

        var result = await service.EnsureInitializedAsync("dashboard");

        Assert.Equal(created.Id, result.Id);
        Assert.NotEqual(first.Id, result.Id);
    }

    [Fact]
    public async Task GetActiveWorkspaceAsync_NeverInitialized_Throws() =>
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(out _).GetActiveWorkspaceAsync());

    [Fact]
    public async Task CreateWorkspaceAsync_BlankName_FallsBackToDefaultName()
    {
        var service = CreateService(out _);

        var workspace = await service.CreateWorkspaceAsync("   ", "dashboard");

        Assert.Equal("Default", workspace.Name);
    }

    [Fact]
    public async Task DuplicateWorkspaceAsync_CopiesLayoutUnderANewId()
    {
        var service = CreateService(out _);
        var original = await service.CreateWorkspaceAsync("Reception", "customers");

        var copy = await service.DuplicateWorkspaceAsync(original.Id, "Reception Copy");

        Assert.NotEqual(original.Id, copy.Id);
        Assert.Equal("Reception Copy", copy.Name);
        Assert.Equal(original.PrimaryModuleId, copy.PrimaryModuleId);
        Assert.False(copy.IsDefault);
    }

    [Fact]
    public async Task RenameWorkspaceAsync_UpdatesNameInWorkspacesList()
    {
        var service = CreateService(out _);
        var workspace = await service.CreateWorkspaceAsync("Reception", "customers");

        await service.RenameWorkspaceAsync(workspace.Id, "Front Desk");

        var all = await service.GetWorkspacesAsync();
        Assert.Contains(all, w => w.Id == workspace.Id && w.Name == "Front Desk");
    }

    [Fact]
    public async Task DeleteWorkspaceAsync_OnlyRemainingWorkspace_Throws()
    {
        var service = CreateService(out _);
        var workspace = await service.EnsureInitializedAsync("dashboard");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteWorkspaceAsync(workspace.Id));
    }

    [Fact]
    public async Task DeleteWorkspaceAsync_TheActiveOne_ReassignsActiveToTheDefault()
    {
        var service = CreateService(out _);
        var defaultWorkspace = await service.EnsureInitializedAsync("dashboard");
        var reception = await service.CreateWorkspaceAsync("Reception", "customers");
        await service.SwitchWorkspaceAsync(reception.Id);

        await service.DeleteWorkspaceAsync(reception.Id);

        var active = await service.GetActiveWorkspaceAsync();
        Assert.Equal(defaultWorkspace.Id, active.Id);
    }

    [Fact]
    public async Task SwitchWorkspaceAsync_UnknownId_Throws() =>
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(out _).SwitchWorkspaceAsync("missing"));

    [Fact]
    public async Task SwitchWorkspaceAsync_RecordsItAsMostRecent()
    {
        var service = CreateService(out _);
        await service.EnsureInitializedAsync("dashboard");
        var reception = await service.CreateWorkspaceAsync("Reception", "customers");

        await service.SwitchWorkspaceAsync(reception.Id);

        var recent = await service.GetRecentWorkspacesAsync();
        Assert.Equal(reception.Id, recent[0].Id);
    }

    [Fact]
    public async Task SaveLayoutAsync_PersistsTheGivenLayout()
    {
        var service = CreateService(out _);
        var workspace = await service.EnsureInitializedAsync("dashboard");
        var split = new PaneSplitDto("split-1", PaneOrientation.Horizontal, 0.5,
            new PaneLeafDto("leaf-1", ["dashboard"], "dashboard"),
            new PaneLeafDto("leaf-2", ["customers"], "customers"));

        await service.SaveLayoutAsync(workspace with { SecondaryRoot = split });

        var reloaded = await service.GetActiveWorkspaceAsync();
        var reloadedSplit = Assert.IsType<PaneSplitDto>(reloaded.SecondaryRoot);
        Assert.Equal("split-1", reloadedSplit.Id);
    }

    [Fact]
    public async Task ResetWorkspaceAsync_ClearsSecondaryPanesDockedPanelsAndFloatingWindows()
    {
        var service = CreateService(out _);
        var workspace = await service.EnsureInitializedAsync("dashboard");
        var leaf = new PaneLeafDto("leaf-1", ["customers"], "customers");
        await service.SaveLayoutAsync(workspace with
        {
            SecondaryRoot = leaf,
            DockedPanels = [new DockedPanelDto("outline", DockSide.Right, 280, true)],
            FloatingWindows = [new FloatingWindowDto("fw-1", "bookings", 0, 0, 800, 600, false)],
        });

        var reset = await service.ResetWorkspaceAsync(workspace.Id);

        Assert.Null(reset.SecondaryRoot);
        Assert.Empty(reset.DockedPanels);
        Assert.Empty(reset.FloatingWindows);
    }
}
