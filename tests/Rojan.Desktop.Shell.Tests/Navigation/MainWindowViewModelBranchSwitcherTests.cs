using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Infrastructure.Organizations;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Shell;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>
/// Exercises <see cref="MainWindowViewModel"/>'s enterprise Branch
/// Switcher - organization grouping, search filtering, and resolving
/// Favorite/Recently-used branch ids into <see cref="BranchDto"/>
/// instances. Uses the real <see cref="FakeOrganizationRepository"/> seed
/// data (org-1 "ROJAN Beauty Group" with branch-1 Downtown/branch-2
/// Uptown, org-2 "Luxe Salon Collective" with branch-3 Luxe Central) so
/// grouping/search assertions are against known, real content.
/// </summary>
public sealed class MainWindowViewModelBranchSwitcherTests
{
    private static ModuleDescriptor Module(string id) =>
        new(new ModuleMetadata(id, id, string.Empty, 0), _ => new Presentation.ViewModels.Modules.PlaceholderModuleViewModel(id));

    private static MainWindowViewModel CreateViewModel(StubCurrentSessionService session) =>
        new(
            new StubModuleRegistry([Module("dashboard")]),
            new StubNavigationService(),
            new PermissionEngine(),
            session,
            new OrganizationQueryService(new FakeOrganizationRepository()),
            TestHelpServices.QueryService,
            TestHelpServices.ContentResolver,
            TestHelpServices.SearchService,
            TestHelpServices.CreateFavoritesStore(),
            TestHelpServices.CreateRecentlyViewedStore(),
            TestNotificationServices.CreateNotificationService(),
            TestNotificationServices.ContentResolver,
            TestNotificationServices.SearchService,
            TestNotificationServices.ToastDismissScheduler,
            TestSearchServices.IndexService,
            TestSearchServices.RankingService,
            TestSearchServices.CreateHistoryStore(),
            TestSearchServices.CreateFavoritesStore());

    [Fact]
    public void BranchGroups_LoadsEveryOrganizationWithItsOwnBranchesOnly()
    {
        var viewModel = CreateViewModel(new StubCurrentSessionService());

        Assert.Equal(2, viewModel.BranchGroups.Count);
        var org1Group = viewModel.BranchGroups.Single(g => g.OrganizationId == "org-1");
        var org2Group = viewModel.BranchGroups.Single(g => g.OrganizationId == "org-2");
        Assert.Equal(2, org1Group.Branches.Count);
        Assert.Single(org2Group.Branches);
        Assert.All(org1Group.Branches, b => Assert.Equal("org-1", b.OrganizationId));
    }

    [Fact]
    public void BranchSearchText_FiltersBranchesByNameAcrossEveryOrganization()
    {
        var viewModel = CreateViewModel(new StubCurrentSessionService());

        viewModel.BranchSearchText = "Downtown";

        var visibleBranches = viewModel.BranchGroups.SelectMany(g => g.Branches).ToList();
        Assert.Single(visibleBranches);
        Assert.Equal("branch-1", visibleBranches[0].Id);
    }

    [Fact]
    public void BranchSearchText_WithNoMatches_LeavesNoGroupsVisible()
    {
        var viewModel = CreateViewModel(new StubCurrentSessionService());

        viewModel.BranchSearchText = "no-such-branch-exists";

        Assert.Empty(viewModel.BranchGroups);
    }

    [Fact]
    public void BranchSearchText_Cleared_RestoresEveryGroup()
    {
        var viewModel = CreateViewModel(new StubCurrentSessionService());
        viewModel.BranchSearchText = "Downtown";

        viewModel.BranchSearchText = string.Empty;

        Assert.Equal(2, viewModel.BranchGroups.Count);
    }

    [Fact]
    public void RecentBranches_ResolvesSessionRecentIdsToBranchDtos()
    {
        var session = new StubCurrentSessionService { RecentBranchIds = ["branch-2", "branch-3"] };

        var viewModel = CreateViewModel(session);

        Assert.Equal(["branch-2", "branch-3"], viewModel.RecentBranches.Select(b => b.Id));
    }

    [Fact]
    public void FavoriteBranches_ResolvesSessionFavoriteIdsToBranchDtos()
    {
        var session = new StubCurrentSessionService { FavoriteBranchIds = ["branch-1"] };

        var viewModel = CreateViewModel(session);

        Assert.Single(viewModel.FavoriteBranches);
        Assert.Equal("branch-1", viewModel.FavoriteBranches[0].Id);
    }

    [Fact]
    public void ToggleBranchSwitcherCommand_TogglesIsBranchSwitcherOpen()
    {
        var viewModel = CreateViewModel(new StubCurrentSessionService());
        Assert.False(viewModel.IsBranchSwitcherOpen);

        viewModel.ToggleBranchSwitcherCommand.Execute(null);
        Assert.True(viewModel.IsBranchSwitcherOpen);

        viewModel.ToggleBranchSwitcherCommand.Execute(null);
        Assert.False(viewModel.IsBranchSwitcherOpen);
    }
}
