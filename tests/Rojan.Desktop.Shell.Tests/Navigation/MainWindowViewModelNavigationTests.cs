using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Infrastructure.Organizations;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Navigation;
using Rojan.Desktop.Presentation.ViewModels.Modules;
using Rojan.Desktop.Shell;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>
/// Exercises <see cref="MainWindowViewModel"/>'s permission-aware
/// Navigation Generation - Phase 22's requirement that the sidebar is
/// built dynamically from permissions, live-refreshing on a branch/role
/// switch (<see cref="Presentation.Organizations.ICurrentSessionService.SessionChanged"/>),
/// while every pre-Phase-22 module (no <see cref="ModuleMetadata.RequiredPermission"/>)
/// stays unconditionally visible - the additive/non-breaking guarantee.
/// </summary>
public sealed class MainWindowViewModelNavigationTests
{
    private static ModuleDescriptor Module(string id, Permission? requiredPermission = null) =>
        new(new ModuleMetadata(id, id, string.Empty, 0, requiredPermission), _ => new PlaceholderModuleViewModel(id));

    private static OrganizationQueryService CreateOrganizationQueryService() => new(new FakeOrganizationRepository());

    private static MainWindowViewModel CreateViewModel(StubModuleRegistry moduleRegistry, StubCurrentSessionService session) =>
        new(
            moduleRegistry,
            new StubNavigationService(),
            new PermissionEngine(),
            session,
            CreateOrganizationQueryService(),
            TestThemeServices.Service,
            TestLocalizationServices.Service,
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
            TestSearchServices.CreateFavoritesStore(),
            TestWorkspaceServices.CreateService(),
            TestWorkspaceServices.FloatingWindowManager,
            TestWorkspaceServices.ServiceProvider);

    [Fact]
    public void NavigationItems_ModuleWithNoRequiredPermission_IsAlwaysVisible()
    {
        var modules = new[] { Module("dashboard") };
        var session = new StubCurrentSessionService { CurrentRole = WorkspaceRole.Support };
        var viewModel = CreateViewModel(new StubModuleRegistry(modules), session);

        Assert.Single(viewModel.NavigationItems);
        Assert.Equal("dashboard", viewModel.NavigationItems[0].Descriptor.Metadata.Id);
    }

    [Fact]
    public void NavigationItems_ModuleRequiringUngrantedPermission_IsHiddenForThatRole()
    {
        var modules = new[] { Module("dashboard"), Module("organizations", Permission.OrganizationManage) };
        var session = new StubCurrentSessionService { CurrentRole = WorkspaceRole.Support };
        var viewModel = CreateViewModel(new StubModuleRegistry(modules), session);

        Assert.DoesNotContain(viewModel.NavigationItems, item => item.Descriptor.Metadata.Id == "organizations");
    }

    [Fact]
    public void NavigationItems_ModuleRequiringGrantedPermission_IsVisibleForThatRole()
    {
        var modules = new[] { Module("dashboard"), Module("organizations", Permission.OrganizationManage) };
        var session = new StubCurrentSessionService { CurrentRole = WorkspaceRole.PlatformOwner };
        var viewModel = CreateViewModel(new StubModuleRegistry(modules), session);

        Assert.Contains(viewModel.NavigationItems, item => item.Descriptor.Metadata.Id == "organizations");
    }

    [Fact]
    public void NavigationItems_AfterRoleSwitchViaSessionChanged_RefreshesLiveWithoutRestart()
    {
        var modules = new[] { Module("dashboard"), Module("organizations", Permission.OrganizationManage) };
        var session = new StubCurrentSessionService { CurrentRole = WorkspaceRole.Support };
        var viewModel = CreateViewModel(new StubModuleRegistry(modules), session);
        Assert.DoesNotContain(viewModel.NavigationItems, item => item.Descriptor.Metadata.Id == "organizations");

        session.CurrentRole = WorkspaceRole.PlatformOwner;
        session.RaiseSessionChanged();

        Assert.Contains(viewModel.NavigationItems, item => item.Descriptor.Metadata.Id == "organizations");
    }

    [Fact]
    public void NavigationItems_AfterRoleSwitchHidesCurrentSelection_FallsBackToFirstVisibleItem()
    {
        var modules = new[] { Module("dashboard"), Module("organizations", Permission.OrganizationManage) };
        var session = new StubCurrentSessionService { CurrentRole = WorkspaceRole.PlatformOwner };
        var viewModel = CreateViewModel(new StubModuleRegistry(modules), session);
        viewModel.SelectedNavigationItem = viewModel.NavigationItems.Single(item => item.Descriptor.Metadata.Id == "organizations");

        session.CurrentRole = WorkspaceRole.Support;
        session.RaiseSessionChanged();

        Assert.Equal("dashboard", viewModel.SelectedNavigationItem.Descriptor.Metadata.Id);
    }

    // ---- Reception Stabilization Sprint: initial-selection soft redirect ----
    // ---- Phase 2B Context State Hardening: same soft redirect, now driven by ContextState ----

    [Fact]
    public void SelectedNavigationItem_NoBusinessContext_DefaultsToAcceptInviteWhenPresent()
    {
        var modules = new[] { Module("dashboard"), Module("accept-invite") };
        var session = new StubCurrentSessionService { ContextState = DesktopContextState.NoBusinessContext };

        var viewModel = CreateViewModel(new StubModuleRegistry(modules), session);

        Assert.Equal("accept-invite", viewModel.SelectedNavigationItem.Descriptor.Metadata.Id);
    }

    [Fact]
    public void SelectedNavigationItem_OwnerContext_DefaultsToFirstItemEvenWhenAcceptInvitePresent()
    {
        var modules = new[] { Module("dashboard"), Module("accept-invite") };
        var session = new StubCurrentSessionService { ContextState = DesktopContextState.OwnerContext };

        var viewModel = CreateViewModel(new StubModuleRegistry(modules), session);

        Assert.Equal("dashboard", viewModel.SelectedNavigationItem.Descriptor.Metadata.Id);
    }

    [Fact]
    public void SelectedNavigationItem_StaffContext_DefaultsToFirstItemEvenWhenAcceptInvitePresent()
    {
        // StaffContext is a real membership exactly like OwnerContext, for this method's purposes.
        var modules = new[] { Module("dashboard"), Module("accept-invite") };
        var session = new StubCurrentSessionService { ContextState = DesktopContextState.StaffContext };

        var viewModel = CreateViewModel(new StubModuleRegistry(modules), session);

        Assert.Equal("dashboard", viewModel.SelectedNavigationItem.Descriptor.Metadata.Id);
    }

    [Fact]
    public void SelectedNavigationItem_DemoContext_DefaultsToAcceptInviteWhenPresent()
    {
        // DemoContext is not a real business context, so it routes exactly like NoBusinessContext here -
        // this method has no reason to distinguish "no context" from "deliberately demoed" context.
        var modules = new[] { Module("dashboard"), Module("accept-invite") };
        var session = new StubCurrentSessionService { ContextState = DesktopContextState.DemoContext };

        var viewModel = CreateViewModel(new StubModuleRegistry(modules), session);

        Assert.Equal("accept-invite", viewModel.SelectedNavigationItem.Descriptor.Metadata.Id);
    }

    [Fact]
    public void SelectedNavigationItem_NoBusinessContextButAcceptInviteNotPresent_FallsBackToFirstItem()
    {
        // Covers a brand-new Owner with no salon yet - also NoBusinessContext, but with
        // no invite token to enter, so this must not trap them behind a page they can't get past.
        var modules = new[] { Module("dashboard"), Module("salon") };
        var session = new StubCurrentSessionService { ContextState = DesktopContextState.NoBusinessContext };

        var viewModel = CreateViewModel(new StubModuleRegistry(modules), session);

        Assert.Equal("dashboard", viewModel.SelectedNavigationItem.Descriptor.Metadata.Id);
    }
}
