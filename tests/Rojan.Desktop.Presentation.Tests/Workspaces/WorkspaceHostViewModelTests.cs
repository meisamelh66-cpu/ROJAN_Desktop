using Rojan.Desktop.Application.Workspaces;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Workspaces;

namespace Rojan.Desktop.Presentation.Tests.Workspaces;

/// <summary>Exercises <see cref="WorkspaceHostViewModel"/>: first-run bootstrap/restore, split/close-tab/cycle, docked-panel toggle, float-out, and workspace create/switch/delete/reset.</summary>
public sealed class WorkspaceHostViewModelTests
{
    private sealed class NoOpViewModel : ViewModelBase
    {
    }

    /// <summary>Every test module's <see cref="ModuleDescriptor.CreateViewModel"/> ignores its <see cref="IServiceProvider"/> parameter (constructs <see cref="NoOpViewModel"/> directly), so this never actually needs to resolve anything - avoids pulling in the full DI container package just for these tests.</summary>
    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private static ModuleDescriptor Module(string id) =>
        new(new ModuleMetadata(id, id, string.Empty, 0), _ => new NoOpViewModel());

    private static WorkspaceHostViewModel CreateHost(
        out FakeWorkspaceRepository repository,
        out StubFloatingWindowManager floatingWindowManager,
        params string[] moduleIds)
    {
        repository = new FakeWorkspaceRepository();
        var workspaceService = new WorkspaceService(repository);
        var moduleRegistry = new StubModuleRegistry(moduleIds.Select(Module).ToList());
        floatingWindowManager = new StubFloatingWindowManager();
        return new WorkspaceHostViewModel(workspaceService, moduleRegistry, new EmptyServiceProvider(), floatingWindowManager);
    }

    [Fact]
    public async Task InitializeAsync_FirstRun_CreatesDefaultWorkspaceWithNoSecondaryPane()
    {
        var host = CreateHost(out _, out _, "dashboard", "customers");

        await host.InitializeAsync("dashboard");

        Assert.Equal("dashboard", host.PrimaryModuleId);
        Assert.False(host.HasSecondaryPane);
        Assert.Single(host.Workspaces);
        Assert.False(host.OutlinePanel.IsVisible);
    }

    [Fact]
    public async Task InitializeAsync_SavedWorkspaceWithDifferentPrimaryModule_RaisesPrimaryModuleChangeRequested()
    {
        var host = CreateHost(out var repository, out _, "dashboard", "customers");
        var saved = new Domain.Workspaces.WorkspaceLayout(
            "w1", "Reception", "customers", null, [], [], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, IsDefault: true);
        await repository.SaveAsync(saved);
        await repository.SetActiveWorkspaceIdAsync("w1");

        string? requestedModuleId = null;
        host.PrimaryModuleChangeRequested += (_, moduleId) => requestedModuleId = moduleId;

        await host.InitializeAsync("dashboard");

        Assert.Equal("customers", requestedModuleId);
    }

    [Fact]
    public async Task SplitRightCommand_NoFocusedPane_ClonesThePrimaryModuleIntoBothSides()
    {
        var host = CreateHost(out _, out _, "dashboard", "customers");
        await host.InitializeAsync("dashboard");

        host.SplitRightCommand.Execute(null);
        await Task.Delay(20);

        Assert.True(host.HasSecondaryPane);
        var split = Assert.IsType<PaneSplitViewModel>(host.SecondaryRoot);
        var first = Assert.IsType<PaneLeafViewModel>(split.First);
        var second = Assert.IsType<PaneLeafViewModel>(split.Second);
        Assert.Equal("dashboard", first.ActiveTab!.ModuleId);
        Assert.Equal("dashboard", second.ActiveTab!.ModuleId);
    }

    [Fact]
    public async Task ClosingTheOnlyTabInOneSplitSide_CollapsesTheSplitIntoTheRemainingSideAlone()
    {
        var host = CreateHost(out _, out _, "dashboard", "customers");
        await host.InitializeAsync("dashboard");
        host.SplitRightCommand.Execute(null);
        await Task.Delay(20);
        var split = (PaneSplitViewModel)host.SecondaryRoot!;
        var firstLeafId = ((PaneLeafViewModel)split.First).Id;
        var secondLeaf = (PaneLeafViewModel)split.Second;

        secondLeaf.ActiveTab!.CloseCommand.Execute(null);
        await Task.Delay(20);

        // The split collapses into whichever side survives - still a
        // secondary pane (a lone leaf now), not back to "no secondary
        // pane at all": the surviving leaf may be showing something
        // genuinely different from the primary pane by now, so it isn't
        // auto-discarded. Closing that leaf's own last tab too (a
        // separate action) is what returns to HasSecondaryPane == false.
        Assert.True(host.HasSecondaryPane);
        var remainingLeaf = Assert.IsType<PaneLeafViewModel>(host.SecondaryRoot);
        Assert.Equal(firstLeafId, remainingLeaf.Id);
        Assert.Equal("dashboard", remainingLeaf.ActiveTab!.ModuleId);
    }

    [Fact]
    public async Task AddTabCommand_OnASplitPane_OpensAndActivatesTheChosenModule()
    {
        var host = CreateHost(out _, out _, "dashboard", "customers");
        await host.InitializeAsync("dashboard");
        host.SplitRightCommand.Execute(null);
        await Task.Delay(20);
        var split = (PaneSplitViewModel)host.SecondaryRoot!;
        var secondLeaf = (PaneLeafViewModel)split.Second;

        secondLeaf.AddTabCommand.Execute(Module("customers"));
        await Task.Delay(20);

        Assert.Equal(2, secondLeaf.Tabs.Count);
        Assert.Equal("customers", secondLeaf.ActiveTab!.ModuleId);
    }

    [Fact]
    public async Task CycleTabNextCommand_FocusedPaneWithMultipleTabs_ActivatesTheNextOne()
    {
        var host = CreateHost(out _, out _, "dashboard", "customers");
        await host.InitializeAsync("dashboard");
        host.SplitRightCommand.Execute(null);
        await Task.Delay(20);
        var split = (PaneSplitViewModel)host.SecondaryRoot!;
        var secondLeaf = (PaneLeafViewModel)split.Second;
        secondLeaf.AddTabCommand.Execute(Module("customers"));
        await Task.Delay(20);
        secondLeaf.FocusCommand.Execute(null);

        host.CycleTabNextCommand.Execute(null);

        Assert.Equal("dashboard", secondLeaf.ActiveTab!.ModuleId);
    }

    [Fact]
    public async Task ToggleDockPanelCommand_Outline_TogglesItsVisibility()
    {
        var host = CreateHost(out _, out _, "dashboard");
        await host.InitializeAsync("dashboard");
        Assert.False(host.OutlinePanel.IsVisible);

        host.ToggleDockPanelCommand.Execute("outline");
        await Task.Delay(20);

        Assert.True(host.OutlinePanel.IsVisible);
    }

    [Fact]
    public async Task FloatOutFocusedTabCommand_RemovesTabFromPaneAndOpensAFloatingWindow()
    {
        var host = CreateHost(out _, out var floatingWindowManager, "dashboard", "customers");
        await host.InitializeAsync("dashboard");
        host.SplitRightCommand.Execute(null);
        await Task.Delay(20);
        var split = (PaneSplitViewModel)host.SecondaryRoot!;
        var secondLeaf = (PaneLeafViewModel)split.Second;
        secondLeaf.FocusCommand.Execute(null);

        host.FloatOutFocusedTabCommand.Execute(null);
        await Task.Delay(20);

        // Same collapse-to-the-surviving-side reasoning as
        // ClosingTheOnlyTabInOneSplitSide_CollapsesTheSplitIntoTheRemainingSideAlone.
        Assert.True(host.HasSecondaryPane);
        Assert.IsType<PaneLeafViewModel>(host.SecondaryRoot);
        Assert.Single(host.FloatingWindows);
        Assert.Single(floatingWindowManager.OpenedIds);
    }

    [Fact]
    public async Task CreateWorkspace_ThenConfirm_SwitchesToItAndAddsToTheList()
    {
        var host = CreateHost(out _, out _, "dashboard", "customers");
        await host.InitializeAsync("dashboard");

        host.StartCreateCommand.Execute(null);
        host.NewWorkspaceNameText = "Reception";
        host.ConfirmNameCommand.Execute(null);
        await Task.Delay(20);

        Assert.Equal("Reception", host.WorkspaceName);
        Assert.Equal(2, host.Workspaces.Count);
    }

    [Fact]
    public async Task DeleteWorkspaceCommand_CanExecute_FalseWhenOnlyOneWorkspaceRemains()
    {
        var host = CreateHost(out _, out _, "dashboard");
        await host.InitializeAsync("dashboard");

        Assert.False(host.DeleteWorkspaceCommand.CanExecute(host.Workspaces[0]));
    }

    [Fact]
    public async Task ResetWorkspaceCommand_ClearsSecondaryPaneAndDockedPanelVisibility()
    {
        var host = CreateHost(out _, out _, "dashboard", "customers");
        await host.InitializeAsync("dashboard");
        host.SplitRightCommand.Execute(null);
        host.ToggleDockPanelCommand.Execute("outline");
        await Task.Delay(20);

        host.ResetWorkspaceCommand.Execute(null);
        await Task.Delay(20);

        Assert.False(host.HasSecondaryPane);
        Assert.False(host.OutlinePanel.IsVisible);
    }

    [Fact]
    public void SetPrimaryModuleId_BeforeInitializeAsync_UpdatesPropertyWithoutThrowing()
    {
        var host = CreateHost(out _, out _, "dashboard", "customers");

        host.SetPrimaryModuleId("customers");

        Assert.Equal("customers", host.PrimaryModuleId);
    }
}
